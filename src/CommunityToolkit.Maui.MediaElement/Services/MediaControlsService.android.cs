using System.Collections.Concurrent;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Media3.Common;
using AndroidX.Media3.DataSource;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.ExoPlayer.TrackSelection;
using AndroidX.Media3.Session;

namespace CommunityToolkit.Maui.Media.Services;

[SupportedOSPlatform("Android26.0")]
[IntentFilter(["androidx.media3.session.MediaSessionService"])] 
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : MediaSessionService
{
	
	readonly ConcurrentDictionary<string, MediaSessionWrapper> sessions = new();

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		PauseAllPlayersAndStopSelf();
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

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (intent is not null)
		{
			var sid = intent.GetStringExtra("sessionId");
			if (!string.IsNullOrEmpty(sid))
			{
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
		return null;
	}

	MediaSessionWrapper CreateOrGetSession(string sessionId)
	{
		if (sessions.TryGetValue(sessionId, out var existing))
		{
			return existing;
		}

		var audioAttribute = new AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)?
			.SetUsage(C.UsageMedia)?
			.Build();

		var trackSelector = new DefaultTrackSelector(this);
		var trackSelectionParameters = trackSelector.BuildUponParameters()?
			.SetPreferredAudioLanguage(C.LanguageUndetermined)? // Fallback to system language if no preferred language found
			.SetPreferredTextLanguage(C.LanguageUndetermined)? // Fallback to system language if no preferred language found
			.SetIgnoredTextSelectionFlags(C.SelectionFlagAutoselect); // Ignore text tracks that are not explicitly selected by the user
		trackSelector.SetParameters((DefaultTrackSelector.Parameters.Builder?)trackSelectionParameters); // Allows us to select tracks based on user preferences

		var loadControlBuilder = new DefaultLoadControl.Builder();
		loadControlBuilder.SetBufferDurationsMs(
			minBufferMs: 15000,
			maxBufferMs: 50000,
			bufferForPlaybackMs: 2500,
			bufferForPlaybackAfterRebufferMs: 5000); // Custom buffering strategy

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

		var wrapper = new MediaSessionWrapper(sessionId, mediaSession, exoPlayer);
		sessions[sessionId] = wrapper;
		
		return wrapper;
	}
}
