using System.Diagnostics.CodeAnalysis;
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

[IntentFilter(["androidx.media3.session.MediaSessionService"])]
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : Service
{
	const int mediaNotificationId = 1;
	const int stragglerCancelDelayMilliseconds = 500;

	static MediaControlsService? runningService;
	static bool shouldLeaveForegroundWhenCreated;

	readonly WeakEventManager taskRemovedEventManager = new();
	readonly Handler cancelStragglerHandler = new(Looper.MainLooper ?? throw new InvalidOperationException("The main looper cannot be null"));

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

		runningService = this;
		if (shouldLeaveForegroundWhenCreated)
		{
			shouldLeaveForegroundWhenCreated = false;
			LeaveForegroundBeforeStopping();
		}
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

		if (ReferenceEquals(runningService, this))
		{
			runningService = null;
		}

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
		NotificationManagerCompat.From(Platform.AppContext)?.Notify(mediaNotificationId, notificationBuilder.Build());
	}

	[MemberNotNull(nameof(playerNotificationManager))]
	public void SetLegacyNotifications(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(session);
		// Its notification replaces the one of the service (same id): like that one, it opens the app when it is tapped
		playerNotificationManager ??= new PlayerNotificationManager.Builder(Platform.AppContext, mediaNotificationId, "1")
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

	// Android 11 defers an app's cancel while a post of the same notification is still queued (the system delays every post by 200 ms),
	// so a service stopped within that time leaves its foreground notification behind. Leaving the foreground first lets the cancels win.
	// Static because the connection may be gone before the service is connected or even created.
	public static void RequestLeaveForegroundBeforeStopping()
	{
		if (runningService is not null)
		{
			runningService.LeaveForegroundBeforeStopping();
		}
		else
		{
			shouldLeaveForegroundWhenCreated = true;
		}
	}

	public static void ForgetRequestToLeaveForeground() => shouldLeaveForegroundWhenCreated = false;

	public void LeaveForegroundBeforeStopping()
	{
		playerNotificationManager?.SetPlayer(null);
		legacyNotificationPlayer = null;
		StopForeground(StopForegroundFlags.Detach);
		NotificationManager?.Cancel(mediaNotificationId);
		cancelStragglerHandler.PostDelayed(() => NotificationManager?.Cancel(mediaNotificationId), stragglerCancelDelayMilliseconds);
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
				StartForeground(mediaNotificationId, notification, ForegroundService.TypeMediaPlayback);
			}
		}
		else
		{
			StartForeground(mediaNotificationId, notificationBuilder.Build());
		}
	}
}