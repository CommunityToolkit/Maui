using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Services;
using Microsoft.Win32.SafeHandles;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
class MediaControlsService : Service
{
	bool isDisposed;
	readonly SafeHandle safeHandle = new SafeFileHandle(IntPtr.Zero, true);

	public NotificationCompat.Builder? Notification;
	public MediaSession? Session;
	public AndroidX.Media3.ExoPlayer.IExoPlayer? Player;
	PlayerNotificationManager? playerNotificationManager;
	NotificationManager? notificationManager;

	public PlayerView? PlayerView { get; set; }

	public BoundServiceBinder? Binder = null;
	
	public override IBinder? OnBind(Intent? intent)
	{
		Binder = new BoundServiceBinder(this);
		return Binder;
	}

	public override StartCommandResult OnStartCommand([NotNull] Intent? intent, StartCommandFlags flags, int startId)
	{
		ArgumentNullException.ThrowIfNull(intent);
		StartForegroundServices();
		return StartCommandResult.NotSticky;
	}
	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		Session?.Release();
		Session?.Dispose();
		notificationManager?.CancelAll();
		notificationManager?.Dispose();
		playerNotificationManager?.SetPlayer(null);
		playerNotificationManager?.Dispose();
		notificationManager = null;
		playerNotificationManager = null;
		Player?.Stop();
		StopSelf();
	}

	public override void OnDestroy()
	{
		StopSelf();
		StopService(new Intent(Platform.AppContext, typeof(MediaControlsService)));
		playerNotificationManager?.SetPlayer(null);
		playerNotificationManager?.SetMediaSessionToken(null);
		playerNotificationManager?.SetSmallIcon(0);
		playerNotificationManager?.SetPriority(NotificationCompat.VisibilitySecret);
		playerNotificationManager?.SetShowPlayButtonIfPlaybackIsSuppressed(false);
		playerNotificationManager?.SetVisibility(NotificationCompat.VisibilitySecret);
		notificationManager?.CancelAll();
		base.OnDestroy();
	}


	protected override void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				safeHandle.Dispose();
			}
			isDisposed = true;
		}
		base.Dispose(disposing);
	}

	static void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var channel = new NotificationChannel("1", "notification", NotificationImportance.Low);
			notificationMnaManager.CreateNotificationChannel(channel);
		}
	}

	void StartForegroundServices()
	{
		notificationManager ??= GetSystemService(NotificationService) as NotificationManager;
		Notification ??= new NotificationCompat.Builder(Platform.AppContext, "1");
		Notification.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		Notification.SetAutoCancel(false);
		Notification.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
		Notification.SetVisibility(NotificationCompat.VisibilityPublic);
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(notificationManager);
			CreateNotificationChannel(notificationManager);
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			ArgumentNullException.ThrowIfNull(Notification);
			StartForeground(1, Notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(Notification);
			StartForeground(1, Notification.Build());
		}
	}
	public void UpdateNotifications()
	{
		ArgumentNullException.ThrowIfNull(Player);
		ArgumentNullException.ThrowIfNull(Session);
		ArgumentNullException.ThrowIfNull(Notification);

		var style = new MediaStyleNotificationHelper.MediaStyle(Session);
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			style.SetShowActionsInCompactView(0, 1, 2);
		}
		else
		{
			SetLegacyNotifications();
		}
		Notification.SetStyle(style);
		NotificationManagerCompat.From(Platform.AppContext).Notify(1, Notification.Build());
	}
	public void SetLegacyNotifications()
	{
		ArgumentNullException.ThrowIfNull(Player);
		ArgumentNullException.ThrowIfNull(Session);
		playerNotificationManager ??= new PlayerNotificationManager.Builder(Platform.AppContext, 1, "1").Build();

		ArgumentNullException.ThrowIfNull(playerNotificationManager);
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
		playerNotificationManager.SetMediaSessionToken(Session.SessionCompatToken);
		playerNotificationManager.SetPlayer(Player);
		playerNotificationManager.SetColorized(true);
		playerNotificationManager.SetShowPlayButtonIfPlaybackIsSuppressed(true);
		playerNotificationManager.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		playerNotificationManager.SetPriority(NotificationCompat.PriorityDefault);
		playerNotificationManager.SetUseChronometer(true);
	}
}