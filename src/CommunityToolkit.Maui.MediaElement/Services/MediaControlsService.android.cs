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
using Java.Util;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[SupportedOSPlatform("Android26.0")]
[IntentFilter(["androidx.media3.session.MediaSessionService"])]
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : MediaSessionService
{
	const string cHANNEL_ID = "media_playback_channel";
	const int nOTIFICATION_ID = 1001;

	MediaSession? mediaSession;
	public IExoPlayer? ExoPlayer;

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		PauseAllPlayersAndStopSelf();
	}

	public NotificationManager? NotificationManager { get; set; }


	public override void OnCreate()
	{
		base.OnCreate();
		CreateNotificationChannel();

		StartForeground(nOTIFICATION_ID, CreateNotification());

		var audioAttribute = new AndroidX.Media3.Common.AudioAttributes.Builder()?
			.SetContentType(C.AudioContentTypeMusic)? // When phonecalls come in, music is paused
			.SetUsage(C.UsageMedia)?
			.Build();

		var trackSelector = new DefaultTrackSelector(this);
		var trackSelectionParameters = trackSelector.BuildUponParameters()?
			.SetPreferredAudioLanguage(C.LanguageUndetermined)? // Fallback to system language if no preferred language found
			.SetPreferredTextLanguage(C.LanguageUndetermined)? // Fallback to system language if no preferred language found
			.SetIgnoredTextSelectionFlags(C.SelectionReasonUnknown); // Ignore text tracks that are not explicitly selected by the user
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
		builder.SetHandleAudioBecomingNoisy(true); // Unplugging headphones will pause playback
		builder.SetLoadControl(loadControlBuilder.Build());
		ExoPlayer = builder.Build() ?? throw new InvalidOperationException("ExoPlayerBuilder.Build() returned null");
	
		var mediaSessionBuilder = new MediaSession.Builder(this, ExoPlayer);
		UUID sessionId = UUID.RandomUUID() ?? throw new InvalidOperationException("UUID.RandomUUID() returned null");
		mediaSessionBuilder.SetId(sessionId.ToString());

		var dataSourceBitmapFactory = new DataSourceBitmapLoader(this);
		mediaSessionBuilder.SetBitmapLoader(dataSourceBitmapFactory);
		mediaSession = mediaSessionBuilder.Build() ?? throw new InvalidOperationException("MediaSession.Builder.Build() returned null");
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
	void CreateNotificationChannel()
	{
		var channel = new Android.App.NotificationChannel(cHANNEL_ID, "Media Playback", NotificationImportance.Low)
		{
			Description = "Media playback controls",
			Name = "Media Playback"
		};
		channel.SetShowBadge(false);
		channel.LockscreenVisibility = NotificationVisibility.Public;
		NotificationManager manager = GetSystemService(NotificationService) as NotificationManager ?? throw new InvalidOperationException($"{nameof(NotificationManager)} cannot be null");
		manager.CreateNotificationChannel(channel);
	}

	static Notification CreateNotification()
	{
		return new NotificationCompat.Builder(Platform.AppContext ?? throw new InvalidOperationException("AppContext cannot be null"), cHANNEL_ID)
			.SetContentTitle("Playing Media")?
			.SetContentText("Artist")?
			.SetSmallIcon(Resource.Drawable.notification_bg_low)?
			.SetVisibility((int)NotificationVisibility.Public)?
			.SetOngoing(true)?
			.Build() ?? throw new InvalidOperationException("Notification cannot be null");
	}

	public override MediaSession? OnGetSession(MediaSession.ControllerInfo? p0)
	{
		return mediaSession;
	}

}