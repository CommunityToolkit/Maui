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
[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
sealed partial class MediaControlsService : Service
{
	readonly WeakEventManager taskRemovedEventManager = new();

	bool isDisposed;

	PlayerNotificationManager? playerNotificationManager;
	NotificationCompat.Builder? notification;

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
		NotificationManager?.CancelAll();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();

		playerNotificationManager?.SetPlayer(null);
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

	[MemberNotNull(nameof(NotificationManager), nameof(notification))]
	public void UpdateNotifications(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(notification);
		ArgumentNullException.ThrowIfNull(NotificationManager);

		var style = new MediaStyleNotificationHelper.MediaStyle(session);
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			SetLegacyNotifications(session, mediaElement);
		}

		notification.SetStyle(style);
		NotificationManagerCompat.From(Platform.AppContext).Notify(1, notification.Build());
	}

	[MemberNotNull(nameof(playerNotificationManager))]
	public void SetLegacyNotifications(in MediaSession session, in PlatformMediaElement mediaElement)
	{
		ArgumentNullException.ThrowIfNull(session);
		playerNotificationManager ??= new PlayerNotificationManager.Builder(Platform.AppContext, 1, "1").Build()
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
		playerNotificationManager.SetMediaSessionToken(session.SessionCompatToken);
		playerNotificationManager.SetPlayer(mediaElement);
		playerNotificationManager.SetColorized(true);
		playerNotificationManager.SetShowPlayButtonIfPlaybackIsSuppressed(true);
		playerNotificationManager.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		playerNotificationManager.SetPriority(NotificationCompat.PriorityDefault);
		playerNotificationManager.SetUseChronometer(true);
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

	[MemberNotNull(nameof(notification), nameof(NotificationManager))]
	void StartForegroundServices()
	{
		NotificationManager ??= GetSystemService(NotificationService) as NotificationManager ?? throw new InvalidOperationException($"{nameof(NotificationManager)} cannot be null");
		notification ??= new NotificationCompat.Builder(Platform.AppContext, "1");

		notification.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		notification.SetAutoCancel(false);
		notification.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
		notification.SetVisibility(NotificationCompat.VisibilityPublic);

		CreateNotificationChannel(NotificationManager);

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			StartForeground(1, notification.Build(), ForegroundService.TypeMediaPlayback);
		}
		else
		{
			StartForeground(1, notification.Build());
		}
	}
}