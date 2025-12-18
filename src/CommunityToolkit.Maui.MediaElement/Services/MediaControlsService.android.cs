using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.TrackSelection;
using AndroidX.Media3.Session;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[SupportedOSPlatform("Android26.0")]
[IntentFilter(["androidx.media3.session.MediaSessionService"])] 
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : MediaSessionService
{
	const string cHANNEL_ID = "media_playback_channel";
	const int base_NOTIFICATION_ID = 1001; // base id; per-session ids will be offsets from this
	
	NotificationManager? notificationManager;
	readonly ConcurrentDictionary<string, MediaSessionWrapper> sessions = new();
	string? currentForegroundSessionId;

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		PauseAllPlayersAndStopSelf();
	}


	public override void OnCreate()
	{
		base.OnCreate();
		CreateNotificationChannel();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			PauseAllPlayersAndStopSelf();
			foreach (var kv in sessions)
			{
				kv.Value.Dispose();
			}
			sessions.Clear();
		}
		base.Dispose(disposing);
	}
	
	public override void OnDestroy()
	{
		base.OnDestroy();
		PauseAllPlayersAndStopSelf();
	}

	public override void OnRebind(Intent? intent)
	{
		base.OnRebind(intent);
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (intent is not null)
		{
			var sid = intent.GetStringExtra("sessionId");
			if (!string.IsNullOrEmpty(sid))
			{
				// ensure session exists
				CreateOrGetSession(sid);
				return StartCommandResult.StickyCompatibility;
			}

			var removeSid = intent.GetStringExtra("removeSessionId");
			if (!string.IsNullOrEmpty(removeSid))
			{
				if (sessions.TryRemove(removeSid, out var wrapper))
				{
					wrapper.Dispose();
				}
				if (sessions.IsEmpty)
				{
					StopSelf();
				}
				return StartCommandResult.NotSticky;
			}
		}

		return base.OnStartCommand(intent, flags, startId);
	}
	void CreateNotificationChannel()
	{
		var channel = new Android.App.NotificationChannel(cHANNEL_ID, "Media Playback", NotificationImportance.Low)
		{
			Description = "Media playback controls",
			Name = "Media Playback"
		};
		channel.SetShowBadge(false);
		channel.LockscreenVisibility = NotificationVisibility.Public;
		notificationManager = GetSystemService(NotificationService) as NotificationManager ?? throw new InvalidOperationException($"{nameof(NotificationManager)} cannot be null");
		notificationManager.CreateNotificationChannel(channel);
	}

	public override MediaSession? OnGetSession(MediaSession.ControllerInfo? controllerInfo)
	{
		var hints = controllerInfo?.ConnectionHints;
		if (hints is not null)
		{
			var sid = hints.GetString("sessionId");
			if (!string.IsNullOrEmpty(sid) && sessions.TryGetValue(sid, out var wrapper))
			{
				return wrapper.Session;
			}
		}

		// Fallback: if there's a current foreground session return it
		if (!string.IsNullOrEmpty(currentForegroundSessionId) && sessions.TryGetValue(currentForegroundSessionId, out var fgWrapper))
		{
			return fgWrapper.Session;
		}
		System.Diagnostics.Debug.WriteLine($"[MediaControlsService] OnGetSession: no session found to return");
		// If no session found, return null — the platform will handle it or client should request creation first
		return null;
	}

	// Create or get a MediaSessionWrapper for a given session id
	MediaSessionWrapper CreateOrGetSession(string sessionId)
	{
		if (sessions.TryGetValue(sessionId, out var existing))
		{
			System.Diagnostics.Debug.WriteLine($"[MediaControlsService] Retrieved existing session for id '{sessionId}'");
			return existing;
		}

		// Build a new ExoPlayer and MediaSession for this sessionId
		var audioAttribute = new AndroidX.Media3.Common.AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)?
			.SetUsage(C.UsageMedia)?
			.Build();

		var trackSelector = new DefaultTrackSelector(this);
		var loadControlBuilder = new DefaultLoadControl.Builder();
		loadControlBuilder.SetBufferDurationsMs(
			minBufferMs: 15000,
			maxBufferMs: 50000,
			bufferForPlaybackMs: 2500,
			bufferForPlaybackAfterRebufferMs: 5000);

		var builder = new ExoPlayerBuilder(this) ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");
		builder.SetTrackSelector(trackSelector);
		builder.SetAudioAttributes(audioAttribute, true);
		builder.SetHandleAudioBecomingNoisy(true);
		builder.SetLoadControl(loadControlBuilder.Build());
		var exoPlayer = builder.Build() ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");

		var mediaSessionBuilder = new MediaSession.Builder(this, exoPlayer);
		mediaSessionBuilder.SetId(sessionId);
		var dataSourceBitmapFactory = new DataSourceBitmapLoader(this);
		mediaSessionBuilder.SetBitmapLoader(dataSourceBitmapFactory);
		var mediaSession = mediaSessionBuilder.Build() ?? throw new InvalidOperationException("MediaSession.Builder.Build() returned null");

		var notificationId = base_NOTIFICATION_ID + Math.Abs(sessionId.GetHashCode() % 1000);
		var wrapper = new MediaSessionWrapper(sessionId, mediaSession, exoPlayer, notificationId);
		sessions[sessionId] = wrapper;

		// Listen for player's play state to enforce exclusive audio focus (pause others)
		exoPlayer.AddListener(new MediaPlayerStateListener(sessionId, this));
		System.Diagnostics.Debug.WriteLine($"[MediaControlsService] Created new session for id '{sessionId}'");
		return wrapper;
	}
	
	internal void PromoteToForeground(string sessionId)
	{
		if (!sessions.TryGetValue(sessionId, out var wrapper))
		{
			return;
		}

		// Demote previous
		if (!string.IsNullOrEmpty(currentForegroundSessionId) && currentForegroundSessionId != sessionId
			&& sessions.TryGetValue(currentForegroundSessionId, out var prev) && prev is not null)
		{
			// Post prev as normal notification (not foreground)
			var notifPrev = CreateNotificationForSession(prev);
			notificationManager?.Notify(prev.NotificationId, notifPrev);
			System.Diagnostics.Debug.WriteLine($"[MediaControlsService] Demoted session '{currentForegroundSessionId}' from foreground");
		}

		// Build notification for current session
		var notif = CreateNotificationForSession(wrapper);

		// If already in foreground, use notify() to update; otherwise StartForeground()
		if (currentForegroundSessionId == sessionId)
		{
			// Already foreground, just update the notification
			notificationManager?.Notify(wrapper.NotificationId, notif);
		}
		else if (string.IsNullOrEmpty(currentForegroundSessionId))
		{
			// First time promoting to foreground
			StartForeground(wrapper.NotificationId, notif);
		}
		else
		{
			// Switching foreground sessions—update the existing foreground notification
			notificationManager?.Notify(wrapper.NotificationId, notif);
		}

		currentForegroundSessionId = sessionId;
	}
	static Notification CreateNotificationForSession(MediaSessionWrapper wrapper)
	{
		System.Diagnostics.Debug.WriteLine($"[MediaControlsService] Creating notification for session '{wrapper.Id}'");
		return new NotificationCompat.Builder(Platform.AppContext ?? throw new InvalidOperationException("AppContext cannot be null"), cHANNEL_ID)
			.SetContentTitle("Playing Media")?
			.SetContentText("Artist")?
			.SetSmallIcon(Resource.Drawable.notification_bg_low)?
			.SetVisibility((int)NotificationVisibility.Public)?
			.SetOngoing(true)?
			.Build() ?? throw new InvalidOperationException("Notification cannot be null");
	}
}
