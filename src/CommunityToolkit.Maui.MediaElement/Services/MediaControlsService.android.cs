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

[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
class MediaControlsService : Service
{
	bool isDisposed;

	public MediaSession? Session;
	public AndroidX.Media3.ExoPlayer.IExoPlayer? Player;
	
	public NotificationManager? NotificationManager;
	PlayerNotificationManager? playerNotificationManager;
	NotificationCompat.Builder? notification;

	public PlayerView? PlayerView { get; set; }

	public BoundServiceBinder? Binder = null;
	
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
	{
		return StartCommandResult.NotSticky;
	}

	public override void OnTaskRemoved(Intent? rootIntent)
	{
		base.OnTaskRemoved(rootIntent);
		Player?.Stop();
		playerNotificationManager?.SetPlayer(null);
		NotificationManager?.CancelAll();
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		playerNotificationManager?.SetPlayer(null);
		NotificationManager?.CancelAll();
		if (OperatingSystem.IsAndroidVersionAtLeast(26) && !OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			StopForeground(true);
		}
		StopSelf();
	}
	protected override void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				NotificationManager?.Dispose();
				playerNotificationManager?.Dispose();
				if (OperatingSystem.IsAndroidVersionAtLeast(26) && !OperatingSystem.IsAndroidVersionAtLeast(33))
				{
					StopForeground(true);
				}
				StopSelf();
			}
			isDisposed = true;
		}
		base.Dispose(disposing);
	}

	public override void OnRebind(Intent? intent)
	{
		base.OnRebind(intent);
		StartForegroundServices();
	}

	[MemberNotNull(nameof(NotificationManager))]
	static void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var channel = new NotificationChannel("1", "1", NotificationImportance.Low);
			notificationMnaManager.CreateNotificationChannel(channel);
		}
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
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(NotificationManager);
			CreateNotificationChannel(NotificationManager);
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			ArgumentNullException.ThrowIfNull(notification);
			StartForeground(1, notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(notification);
			StartForeground(1, notification.Build());
		}
	}

	[MemberNotNull(nameof(NotificationManager), nameof(notification))]
	public void UpdateNotifications()
	{
		ArgumentNullException.ThrowIfNull(Player);
		ArgumentNullException.ThrowIfNull(Session);
		ArgumentNullException.ThrowIfNull(notification);

		var style = new MediaStyleNotificationHelper.MediaStyle(Session);
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			SetLegacyNotifications();
		}
		notification.SetStyle(style);
		NotificationManagerCompat.From(Platform.AppContext).Notify(1, notification.Build());
		ArgumentNullException.ThrowIfNull(NotificationManager);
	}

	[MemberNotNull(nameof(playerNotificationManager))]
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