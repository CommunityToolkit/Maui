using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Primitives;
using CommunityToolkit.Maui.Services;
using Microsoft.Win32.SafeHandles;
using Resource = Microsoft.Maui.Controls.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[Service(Exported = false, Enabled = true, Name = "communityToolkit.maui.media.services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
class MediaControlsService : Service
{
	public const string ACTION_PLAY = "MediaAction.play";
	public const string ACTION_PAUSE = "MediaAction.pause";
	public const string ACTION_UPDATE_UI = "CommunityToolkit.Maui.Services.action.UPDATE_UI";
	public const string ACTION_UPDATE_PLAYER = "CommunityToolkit.Maui.Services.action.UPDATE_PLAYER";
	public const string ACTION_REWIND = "MediaAction.rewind";
	public const string ACTION_FASTFORWARD = "MediaAction.fastForward";
	bool isDisposed;
	bool isInitialized = false;
	readonly SafeHandle safeHandle = new SafeFileHandle(IntPtr.Zero, true);
	
	Intent? mediaManagerIntent;
	PendingIntentFlags pendingIntentFlags;
	MediaSessionCompat? mediaSession;
	NotificationCompat.Builder? notification;
	NotificationCompat.Action? actionPlay;
	NotificationCompat.Action? actionPause;
	NotificationCompat.Action? actionNext;
	NotificationCompat.Action? actionPrevious;
	MediaSessionCompat.Token? token;
	NotificationManager? notificationManager;
	ReceiveUpdates? receiveUpdates;
	NotificationService? notificationService;

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand([NotNull] Intent? intent, StartCommandFlags flags, int startId)
	{
		ArgumentNullException.ThrowIfNull(intent);
		StartForegroundService(intent).SafeFireAndForget(ex => System.Diagnostics.Trace.TraceError($"[error] {ex}, {ex.Message}"));
		return StartCommandResult.Sticky;
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
		if(OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			var channel = new NotificationChannel("1", "notification", NotificationImportance.Low);
			notificationMnaManager.CreateNotificationChannel(channel);
		}
	}

	[MemberNotNull(nameof(mediaSession))]
	ValueTask StartForegroundService(Intent mediaManagerIntent, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(mediaManagerIntent);
		this.mediaManagerIntent = mediaManagerIntent;
		if (OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			token ??= (MediaSessionCompat.Token)(mediaManagerIntent.GetParcelableExtra("token", Java.Lang.Class.FromType(typeof(MediaSessionCompat.Token))) ?? throw new InvalidOperationException("Token cannot be null"));
		}
		if (OperatingSystem.IsAndroidVersionAtLeast(21) && !OperatingSystem.IsAndroidVersionAtLeast(33))
		{
#pragma warning disable CA1422 // Validate platform compatibility
			token ??= (MediaSessionCompat.Token)(mediaManagerIntent.GetParcelableExtra("token") ?? throw new InvalidOperationException("Token cannot be null"));
#pragma warning restore CA1422 // Validate platform compatibility
		}
		if (!OperatingSystem.IsAndroidVersionAtLeast(21))
		{
			throw new InvalidOperationException("MediaSessionCompat.Token is not supported on this device");
		}
		mediaSession ??= new MediaSessionCompat(Platform.AppContext, "notification")
		{
			Active = true,
		};

		if(receiveUpdates is null && !OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			receiveUpdates = new ReceiveUpdates();
			IntentFilter intentFilter = new(MediaControlsService.ACTION_UPDATE_UI);
			var flags = ContextCompat.ReceiverNotExported;
			ContextCompat.RegisterReceiver(Platform.AppContext, receiveUpdates, intentFilter, flags);
			ArgumentNullException.ThrowIfNull(IPlatformApplication.Current);
			notificationService = IPlatformApplication.Current.Services.GetService<NotificationService>();
			ArgumentNullException.ThrowIfNull(notificationService);
			notificationService.NotificationReceived += OnReceiveUpdatesPropertyChanged;
		}
		pendingIntentFlags = OperatingSystem.IsAndroidVersionAtLeast(23)
			? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;

		return InitializeNotification(mediaSession, mediaManagerIntent, cancellationToken);
	}

	async ValueTask InitializeNotification(MediaSessionCompat mediaSession, Intent mediaManagerIntent, CancellationToken cancellationToken)
	{
		notificationManager = GetSystemService(NotificationService) as NotificationManager;
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);

		notification ??= new NotificationCompat.Builder(Platform.AppContext, "1");

		await OnSetContent(mediaManagerIntent, cancellationToken).ConfigureAwait(false);
		mediaSession.SetExtras(intent.Extras);
		mediaSession.SetPlaybackToLocal(AudioManager.AudioSessionIdGenerate);
		mediaSession.SetSessionActivity(pendingIntent);

		if (OperatingSystem.IsAndroidVersionAtLeast(26) && notificationManager is not null)
		{
			CreateNotificationChannel(notificationManager);
		}
		
		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			StartForeground(1, notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(26))
		{
			StartForeground(1, notification.Build());
		}
		if(!OperatingSystem.IsAndroidVersionAtLeast(33) && !isInitialized)
		{
			isInitialized = true;
			OnReceiveUpdatesPropertyChanged(this, new NotificationEventArgs(ACTION_PLAY, ACTION_UPDATE_UI));
		}
	}
	async Task OnSetContent(Intent mediaManagerIntent, CancellationToken cancellationToken)
	{
		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);
		if (OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			style.SetShowActionsInCompactView(0, 1, 2);
		}
		if (!OperatingSystem.IsAndroidVersionAtLeast(33))
		{
			OnSetIntents();
			var albumArtUri = mediaManagerIntent.GetStringExtra("albumArtUri") ?? string.Empty;
			var bitmap = await MediaManager.GetBitmapFromUrl(albumArtUri, cancellationToken).ConfigureAwait(false);
			var title = mediaManagerIntent.GetStringExtra("title") ?? string.Empty;
			var artist = mediaManagerIntent.GetStringExtra("artist") ?? string.Empty;
			notification?.SetContentTitle(title);
			notification?.SetContentText(artist);
			notification?.SetLargeIcon(bitmap);
		}
		
		notification?.SetStyle(style);
		notification?.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		notification?.SetAutoCancel(false);
		notification?.SetVisibility(NotificationCompat.VisibilityPublic);
	}

	void OnSetIntents()
	{
		var pause = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		pause.SetAction(MediaControlsService.ACTION_PAUSE);
		var pPause = PendingIntent.GetBroadcast(Platform.AppContext, 1, pause, pendingIntentFlags);
		actionPause ??= new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_pause, MediaControlsService.ACTION_PAUSE, pPause).Build();
		
		var play = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		play.SetAction(MediaControlsService.ACTION_PLAY);
		var pPlay = PendingIntent.GetBroadcast(Platform.AppContext, 1, play, pendingIntentFlags);
		actionPlay ??= new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_play, MediaControlsService.ACTION_PLAY, pPlay).Build();
		
		var previous = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		previous.SetAction(MediaControlsService.ACTION_REWIND);
		var pPrevious = PendingIntent.GetBroadcast(Platform.AppContext, 1, previous, pendingIntentFlags);
		actionPrevious ??= new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_seek_back, MediaControlsService.ACTION_REWIND, pPrevious).Build();

		var next = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		next.SetAction(MediaControlsService.ACTION_UPDATE_PLAYER);
		var pNext = PendingIntent.GetBroadcast(Platform.AppContext, 1, next, pendingIntentFlags);
		actionNext ??= new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_seek_forward, MediaControlsService.ACTION_FASTFORWARD, pNext).Build();
	}

	async void OnReceiveUpdatesPropertyChanged(object? sender, NotificationEventArgs e)
	{
		
		if (notification is null || string.IsNullOrEmpty(e.Action) || e.Sender.Equals(ACTION_UPDATE_PLAYER) || Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			return;
		}
		var action = e.Action;
		
		notification.ClearActions();
		notification.AddAction(actionPrevious);
		switch (action)
		{
			case MediaControlsService.ACTION_PAUSE:
				notification.AddAction(actionPlay);
				break;
			case MediaControlsService.ACTION_PLAY:
				notification.AddAction(actionPause);
				break;
		}
		notification.AddAction(actionNext);
		ArgumentNullException.ThrowIfNull(mediaManagerIntent);
		await OnSetContent(mediaManagerIntent, CancellationToken.None).ConfigureAwait(false);
		NotificationManagerCompat.From(Platform.AppContext).Notify(1, notification.Build());
	}

	public override void OnDestroy()
	{
		notificationManager?.CancelAll();
		if (notificationService is not null)
		{
			notificationService.NotificationReceived -= OnReceiveUpdatesPropertyChanged;
		}
		Platform.CurrentActivity?.StopService(new Intent(Platform.AppContext, typeof(MediaControlsService)));
		base.OnDestroy();
	}
}

[BroadcastReceiver(Enabled = true, Exported = false)]
sealed class ReceiveUpdates : BroadcastReceiver
{
	readonly NotificationService? notificationService;
	public ReceiveUpdates()
	{
		ArgumentNullException.ThrowIfNull(IPlatformApplication.Current);
		notificationService = IPlatformApplication.Current.Services.GetService<NotificationService>();
	}
	public override void OnReceive(Context? context, Intent? intent)
	{

		var action = intent?.Action;
		if (context is null || action is null || notificationService is null)
		{
			return;
		}
		notificationService.Received(action, MediaControlsService.ACTION_UPDATE_UI);
	}
}