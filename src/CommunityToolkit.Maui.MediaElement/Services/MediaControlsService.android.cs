using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Services;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[SupportedOSPlatform("Android26.0")]
[IntentFilter(["androidx.media3.session.MediaSessionService"])]
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : Service
{
	readonly WeakEventManager taskRemovedEventManager = new();

	bool isDisposed;

	PlayerNotificationManager? playerNotificationManager;
	NotificationCompat.Builder? notificationBuilder;

	MediaSession? notificationSession;
	PlatformMediaElement? legacyNotificationPlayer;

	public event EventHandler TaskRemoved
	{
		add => taskRemovedEventManager.AddEventHandler(value);
		remove => taskRemovedEventManager.RemoveEventHandler(value);
	}

	public BoundServiceBinder? Binder { get; private set; }
	public NotificationManager? NotificationManager { get; private set; }

	public override IBinder? OnBind(Intent? intent)
	{
		Binder = new BoundServiceBinder(this);
		return Binder;
	}

	public override void OnCreate()
	{
		base.OnCreate();
		StartForegroundServices();
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
		=> StartCommandResult.NotSticky;

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		taskRemovedEventManager.HandleEvent(this, EventArgs.Empty, nameof(TaskRemoved));

		playerNotificationManager?.SetPlayer(null);
		legacyNotificationPlayer = null;
		NotificationManager?.CancelAll();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();

		playerNotificationManager?.SetPlayer(null);
		legacyNotificationPlayer = null;
		// The binder of a former binding can keep the destroyed service alive: the MediaStyle of its notification must not keep the media
		// session alive with it (and, through the session's player, the MediaElement's platform view)
		notificationBuilder?.SetStyle(null);
		notificationSession = null;
		NotificationManager?.CancelAll();
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			StopForeground(true);
		}

		StopSelf();
	}

	public override void OnRebind(Intent? intent)
	{
		base.OnRebind(intent);
		StartForegroundServices();
	}

	[MemberNotNull(nameof(NotificationManager), nameof(notificationBuilder))]
	public void UpdateNotifications(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(notificationBuilder);
		ArgumentNullException.ThrowIfNull(NotificationManager);

		var style = new MediaStyleNotificationHelper.MediaStyle(session);
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			SetLegacyNotifications(session, mediaElement);
		}

		notificationBuilder.SetStyle(style);
		notificationSession = session;
		NotificationManagerCompat.From(Platform.AppContext)?.Notify(1, notificationBuilder.Build());
	}

	[MemberNotNull(nameof(playerNotificationManager))]
	public void SetLegacyNotifications(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(session);
		// Its notification replaces the one of the service (same id): like that one, it opens the app when it is tapped
		playerNotificationManager ??= new PlayerNotificationManager.Builder(Platform.AppContext, 1, "1")
			.SetMediaDescriptionAdapter(new DefaultMediaDescriptionAdapter(CreateActivityPendingIntent()))
			?.Build()
			?? throw new InvalidOperationException("PlayerNotificationManager cannot be null");

		playerNotificationManager.SetUseFastForwardAction(true);
		playerNotificationManager.SetUseFastForwardActionInCompactView(true);
		playerNotificationManager.SetUseRewindAction(true);
		playerNotificationManager.SetUseRewindActionInCompactView(true);
		playerNotificationManager.SetUseNextAction(true);
		playerNotificationManager.SetUseNextActionInCompactView(true);
		playerNotificationManager.SetUsePlayPauseActions(true);
		playerNotificationManager.SetUsePreviousAction(true);
		playerNotificationManager.SetColor(Resource.Color.abc_primary_text_material_dark);
		playerNotificationManager.SetUsePreviousActionInCompactView(true);
		playerNotificationManager.SetVisibility(NotificationCompat.VisibilityPublic);
		playerNotificationManager.SetMediaSessionToken(session.PlatformToken);
		playerNotificationManager.SetPlayer(mediaElement);
		legacyNotificationPlayer = mediaElement;
		playerNotificationManager.SetColorized(true);
		playerNotificationManager.SetShowPlayButtonIfPlaybackIsSuppressed(true);
		playerNotificationManager.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		playerNotificationManager.SetPriority(NotificationCompat.PriorityDefault);
		playerNotificationManager.SetUseChronometer(true);
	}

	// The service keeps running while another MediaElement is bound to it, so OnDestroy does not run when one of them goes away: the
	// notification must then forget the session and the player of that MediaElement, or it keeps them (and, through the player, the
	// MediaElement's platform view) alive. The notification of another MediaElement is left alone.
	public void ForgetSession(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		if (ReferenceEquals(notificationSession, session))
		{
			notificationBuilder?.SetStyle(null);
			notificationSession = null;
		}

		if (ReferenceEquals(legacyNotificationPlayer, mediaElement))
		{
			playerNotificationManager?.SetPlayer(null);
			legacyNotificationPlayer = null;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				NotificationManager?.Dispose();
				NotificationManager = null;

				playerNotificationManager?.Dispose();
				playerNotificationManager = null;
				legacyNotificationPlayer = null;

				if (!OperatingSystem.IsAndroidVersionAtLeast(33))
				{
					StopForeground(true);
				}

				StopSelf();
			}

			isDisposed = true;
		}

		base.Dispose(disposing);
	}

	static void CreateNotificationChannel(in NotificationManager notificationMnaManager)
	{
		var channel = new NotificationChannel("1", "1", NotificationImportance.Low);
		notificationMnaManager.CreateNotificationChannel(channel);
	}

	static PendingIntent CreateActivityPendingIntent()
	{
		var packageName = Platform.AppContext.PackageName ?? throw new InvalidOperationException("PackageName cannot be null");
		var packageManager = Platform.AppContext.PackageManager ?? throw new InvalidOperationException("PackageManager cannot be null");
		var launchIntent = packageManager.GetLaunchIntentForPackage(packageName) ?? throw new InvalidOperationException("Launch intent cannot be null");

		launchIntent.SetFlags(ActivityFlags.ClearTop | ActivityFlags.SingleTop);

		var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
		return PendingIntent.GetActivity(Platform.AppContext, 0, launchIntent, flags)
			   ?? throw new InvalidOperationException("PendingIntent cannot be null");
	}

	[MemberNotNull(nameof(notificationBuilder), nameof(NotificationManager))]
	void StartForegroundServices()
	{
		NotificationManager ??= GetSystemService(NotificationService) as NotificationManager ?? throw new InvalidOperationException($"{nameof(NotificationManager)} cannot be null");
		notificationBuilder ??= new NotificationCompat.Builder(Platform.AppContext, "1");
		var pendingIntent = CreateActivityPendingIntent();
		notificationBuilder.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		notificationBuilder.SetAutoCancel(false);
		notificationBuilder.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
		notificationBuilder.SetVisibility(NotificationCompat.VisibilityPublic);
		notificationBuilder.SetContentIntent(pendingIntent);

		CreateNotificationChannel(NotificationManager);

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			if (notificationBuilder.Build() is Notification notification)
			{
				StartForeground(1, notification, ForegroundService.TypeMediaPlayback);
			}
		}
		else
		{
			StartForeground(1, notificationBuilder.Build());
		}
	}
}