using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Media3.ExoPlayer;
using AndroidX.Media3.Session;
using AndroidX.Media3.UI;
using CommunityToolkit.Maui.Services;
using Microsoft.Win32.SafeHandles;
using AudioAttributes = AndroidX.Media3.Common.AudioAttributes;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
class MediaControlsService : MediaSessionService
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
		Player = new ExoPlayerBuilder(Platform.AppContext).Build() ?? throw new InvalidOperationException("Player cannot be null");
		Player.SetHandleAudioBecomingNoisy(true);
		Player.SetAudioAttributes(AudioAttributes.Default, true);

		string randomId = Convert.ToBase64String(Guid.NewGuid().ToByteArray())[..8];
		var mediaSessionWRandomId = new AndroidX.Media3.Session.MediaSession.Builder(Platform.AppContext, Player);
		mediaSessionWRandomId.SetId(randomId);
		Session ??= mediaSessionWRandomId.Build();
		ArgumentNullException.ThrowIfNull(Session);

		PlayerView = new PlayerView(Platform.AppContext)
		{
			Player = Player,
			UseController = false,
			ControllerAutoShow = false,
			LayoutParameters = new RelativeLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent)
		};
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
		if (Player?.PlayWhenReady == true)
		{
			Player.Stop();
		}
		StopSelf();
	}

	public override void OnDestroy()
	{
		StopSelf();
		StopService(new Intent(Platform.AppContext, typeof(MediaControlsService)));
		playerNotificationManager?.SetPlayer(null);
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

	public override AndroidX.Media3.Session.MediaSession? OnGetSession(MediaSession.ControllerInfo? p0)
	{	
		return Session;
	}

	void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var id = Session?.Id;
			ArgumentNullException.ThrowIfNull(id);
			var channel = new NotificationChannel(id, id, NotificationImportance.Low);
			notificationMnaManager.CreateNotificationChannel(channel);
		}
	}

	[MemberNotNull(nameof(Session))]
	[MemberNotNull(nameof(Player))]
	void StartForegroundServices()
	{
		ArgumentNullException.ThrowIfNull(Player);
		ArgumentNullException.ThrowIfNull(Session);
		var id = Session.Id ?? throw new InvalidOperationException("Session Id cannot be null");

		notificationManager ??= GetSystemService(NotificationService) as NotificationManager;
		Notification ??= new NotificationCompat.Builder(Platform.AppContext, id);

		ArgumentNullException.ThrowIfNull(Player);
		var style = new MediaStyleNotificationHelper.MediaStyle(Session);
		Notification.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		Notification.SetAutoCancel(false);
		Notification.SetForegroundServiceBehavior(NotificationCompat.ForegroundServiceImmediate);
		Notification.SetVisibility(NotificationCompat.VisibilityPublic);
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			style.SetShowActionsInCompactView(0, 1, 2);
		}
		else
		{
			SetLegacyNotifications();
		}
		Notification.SetStyle(style);
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(notificationManager);
			CreateNotificationChannel(notificationManager);
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			ArgumentNullException.ThrowIfNull(Notification);
			StartForeground(id.GetHashCode(), Notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}
		
		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			ArgumentNullException.ThrowIfNull(Notification);
			StartForeground(id.GetHashCode(), Notification.Build());
		}
	}
	
	void SetLegacyNotifications()
	{
		ArgumentNullException.ThrowIfNull(Player);
		ArgumentNullException.ThrowIfNull(Session);
		var id = Session.Id;
		ArgumentNullException.ThrowIfNull(id);
		playerNotificationManager ??= new PlayerNotificationManager.Builder(Platform.AppContext, id.GetHashCode(), id).Build();

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