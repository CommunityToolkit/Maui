using System.ComponentModel;
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

	readonly SafeHandle safeHandle = new SafeFileHandle(IntPtr.Zero, true);
	
	bool isDisposed;

	PendingIntentFlags pendingIntentFlags;
	MediaSessionCompat? mediaSession;
	NotificationCompat.Builder? notification;
	NotificationCompat.Action? actionPlay;
	NotificationCompat.Action? actionPause;
	NotificationCompat.Action? actionNext;
	NotificationCompat.Action? actionPrevious;
	MediaSessionCompat.Token? token;
	ReceiveUpdates? receiveUpdates;

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
			
			mediaSession?.Release();
			mediaSession?.Dispose();
			mediaSession = null;

			receiveUpdates?.Dispose();
			receiveUpdates = null;
			isDisposed = true;
		}
		base.Dispose(disposing);
	}

	static void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		var channel = new NotificationChannel("1", "notification", NotificationImportance.Low);
		notificationMnaManager.CreateNotificationChannel(channel);
	}

	[MemberNotNull(nameof(mediaSession))]
	[MemberNotNull(nameof(token))]
	[MemberNotNull(nameof(receiveUpdates))]
	ValueTask StartForegroundService(Intent mediaManagerIntent, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(mediaManagerIntent);
		token ??= (MediaSessionCompat.Token)(mediaManagerIntent.GetParcelableExtra("token") ?? throw new InvalidOperationException("Token cannot be null"));

		mediaSession ??= new MediaSessionCompat(Platform.AppContext, "notification")
		{
			Active = true,
		};

		if(receiveUpdates is null)
		{
			receiveUpdates = new ReceiveUpdates();
			IntentFilter intentFilter = new(MediaControlsService.ACTION_UPDATE_UI);
			var flags = ContextCompat.ReceiverNotExported;
			ContextCompat.RegisterReceiver(Platform.AppContext, receiveUpdates, intentFilter, flags);
			ReceiveUpdates.PropertyChanged += OnReceiveUpdatesPropertyChanged;
		}
		pendingIntentFlags = OperatingSystem.IsAndroidVersionAtLeast(23)
			? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;

		return InitializeNotification(mediaSession, mediaManagerIntent, cancellationToken);
	}

	async ValueTask InitializeNotification(MediaSessionCompat mediaSession, Intent mediaManagerIntent, CancellationToken cancellationToken)
	{
		var notificationManager = GetSystemService(NotificationService) as NotificationManager;
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);

		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);

		if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
		{
			style.SetShowActionsInCompactView(0, 1, 2);
		}
		if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu
			&& notification is null)
		{
			notification = new NotificationCompat.Builder(Platform.AppContext, "1");
			OnSetIntents();
			await OnSetContent(mediaManagerIntent, cancellationToken).ConfigureAwait(false);
		}

		notification ??= new NotificationCompat.Builder(Platform.AppContext, "1");
		notification.SetStyle(style);
		notification.SetSmallIcon(Resource.Drawable.media3_notification_small_icon);
		notification.SetAutoCancel(false);
		notification.SetVisibility(NotificationCompat.VisibilityPublic);
		mediaSession.SetExtras(intent.Extras);
		mediaSession.SetPlaybackToLocal(AudioManager.AudioSessionIdGenerate);
		mediaSession.SetSessionActivity(pendingIntent);

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O && notificationManager is not null)
		{
			CreateNotificationChannel(notificationManager);
		}
		
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
		{
			StartForeground(1, notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			StartForeground(1, notification.Build());
		}
	}
	async Task OnSetContent(Intent mediaManagerIntent, CancellationToken cancellationToken)
	{
		var albumArtUri = mediaManagerIntent.GetStringExtra("albumArtUri") ?? string.Empty;
		var bitmap = await MediaManager.GetBitmapFromUrl(albumArtUri, cancellationToken).ConfigureAwait(false);
		var title = mediaManagerIntent.GetStringExtra("title") ?? string.Empty;
		var artist = mediaManagerIntent.GetStringExtra("artist") ?? string.Empty;
		notification?.SetContentTitle(title);
		notification?.SetContentText(artist);
		notification?.SetLargeIcon(bitmap);
	}

	void OnSetIntents()
	{
		var pause = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		pause.SetAction(MediaControlsService.ACTION_PAUSE);
		pause.PutExtra("ACTION", MediaControlsService.ACTION_PAUSE);
		var pPause = PendingIntent.GetBroadcast(Platform.AppContext, 1, pause, pendingIntentFlags);
		actionPause = new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_pause, MediaControlsService.ACTION_PAUSE, pPause).Build();

		var play = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		play.SetAction(MediaControlsService.ACTION_PLAY);
		play.PutExtra("ACTION", MediaControlsService.ACTION_PLAY);
		var pPlay = PendingIntent.GetBroadcast(Platform.AppContext, 1, play, pendingIntentFlags);
		actionPlay = new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_play, MediaControlsService.ACTION_PLAY, pPlay).Build();

		var previous = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		previous.SetAction(MediaControlsService.ACTION_REWIND);
		previous.PutExtra("ACTION", MediaControlsService.ACTION_REWIND);
		var pPrevious = PendingIntent.GetBroadcast(Platform.AppContext, 1, previous, pendingIntentFlags);
		actionPrevious = new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_seek_back, MediaControlsService.ACTION_REWIND, pPrevious).Build();

		var next = new Intent(Platform.AppContext, typeof(UIUpdateReceiver));
		next.SetAction(MediaControlsService.ACTION_UPDATE_PLAYER);
		next.PutExtra("ACTION", MediaControlsService.ACTION_FASTFORWARD);
		var pNext = PendingIntent.GetBroadcast(Platform.AppContext, 1, next, pendingIntentFlags);
		actionNext = new NotificationCompat.Action.Builder(Resource.Drawable.media3_notification_seek_forward, MediaControlsService.ACTION_FASTFORWARD, pNext).Build();
	}

	void OnReceiveUpdatesPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (notification is null || string.IsNullOrEmpty(e.PropertyName))
		{
			return;
		}
		var action = e.PropertyName;

		notification.ClearActions();
		notification.AddAction(actionPrevious);
		switch (action)
		{
			case MediaControlsService.ACTION_PLAY:
				notification.AddAction(actionPlay);
				break;
			case MediaControlsService.ACTION_PAUSE:
				notification.AddAction(actionPause);
				break;
		}
		notification.AddAction(actionNext);
		notification.Build();
	}

	public override void OnDestroy()
	{
		Platform.CurrentActivity?.StopService(new Intent(Platform.AppContext, typeof(MediaControlsService)));
		base.OnDestroy();
	}
}

[BroadcastReceiver(Enabled = true, Exported = false)]
class ReceiveUpdates : BroadcastReceiver
{
	public static event EventHandler<PropertyChangedEventArgs>? PropertyChanged;
	public ReceiveUpdates()
	{
	}
	public override void OnReceive(Context? context, Intent? intent)
	{
		
		var action = intent?.Action;
		if(context is null)
		{
			return;
		}
		var args = new PropertyChangedEventArgs(action);
		PropertyChanged?.Invoke(null, args);
	}
}