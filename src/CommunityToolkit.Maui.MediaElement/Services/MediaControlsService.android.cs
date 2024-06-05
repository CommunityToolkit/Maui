using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.Media.Session;
using AndroidX.Core.App;
using AndroidX.LocalBroadcastManager.Content;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Win32.SafeHandles;
using AndroidStream = Android.Media.Stream;
using Resource = Microsoft.Maui.Resource;

namespace CommunityToolkit.Maui.Media.Services;

[Service(Exported = false, Enabled = true, Name = "CommunityToolkit.Maui.Media.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service
{
	public const string ACTION_PLAY = "MediaAction.play";
	public const string ACTION_PAUSE = "MediaAction.pause";
	public const string ACTION_UPDATE_UI = "CommunityToolkit.Maui.Services.action.UPDATE_UI";
	public const string ACTION_UPDATE_PLAYER = "CommunityToolkit.Maui.Services.action.UPDATE_PLAYER";
	public const string ACTION_REWIND = "MediaAction.rewind";
	public const string ACTION_FASTFORWARD = "MediaAction.fastForward";

	bool isDisposed;

	PendingIntentFlags pendingIntentFlags;
	SafeHandle? safeHandle = new SafeFileHandle(IntPtr.Zero, true);
	MediaSessionCompat? mediaSession;
	AudioManager? audioManager;
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

		if (!string.IsNullOrEmpty(intent.Action) && receiveUpdates is not null)
		{
			BroadcastUpdate(ACTION_UPDATE_PLAYER, intent.Action);
		}

		StartForegroundService(intent).AsTask().ContinueWith(t =>
		{
			if (t.Exception is not null)
			{
				foreach (var exception in t.Exception.InnerExceptions)
				{
					System.Diagnostics.Trace.WriteLine($"[error] {exception}, {exception.Message}");
				}
			}
		}, TaskContinuationOptions.OnlyOnFaulted);

		return StartCommandResult.Sticky;
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

		if (receiveUpdates is null)
		{
			receiveUpdates = new ReceiveUpdates();
			receiveUpdates.PropertyChanged += OnReceiveUpdatesPropertyChanged;
			LocalBroadcastManager.GetInstance(this).RegisterReceiver(receiveUpdates, new IntentFilter(ACTION_UPDATE_UI));
		}

		OnSetupAudioServices();

		pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
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
			style.SetShowActionsInCompactView(0, 1, 2, 3);
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
		notification.SetSmallIcon(_Microsoft.Android.Resource.Designer.Resource.Drawable.exo_styled_controls_audiotrack);
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

	void OnReceiveUpdatesPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (notification is null || string.IsNullOrEmpty(receiveUpdates?.Action))
		{
			return;
		}
		notification.ClearActions();
		notification.AddAction(actionPrevious);
		if (receiveUpdates.Action is ACTION_PLAY)
		{
			notification.AddAction(actionPause);
		}
		if (receiveUpdates.Action is ACTION_PAUSE)
		{
			notification.AddAction(actionPlay);
		}
		notification.AddAction(actionNext);
		notification.Build();
	}

	void OnSetupAudioServices()
	{
		audioManager = GetSystemService(Context.AudioService) as AudioManager;
		ArgumentNullException.ThrowIfNull(audioManager);
		audioManager.RequestAudioFocus(null, AndroidStream.Music, AudioFocus.Gain);
		audioManager.SetParameters("Ducking=true");
		audioManager.SetStreamVolume(AndroidStream.Music, audioManager.GetStreamVolume(AndroidStream.Music), VolumeNotificationFlags.ShowUi);
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
		var pause = new Intent(this, typeof(MediaControlsService));
		pause.SetAction("MediaAction.pause");
		var pPause = PendingIntent.GetService(this, 1, pause, pendingIntentFlags);
		actionPause ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_pause, ACTION_PAUSE, pPause).Build();

		var play = new Intent(this, typeof(MediaControlsService));
		play.SetAction("MediaAction.play");
		var pPlay = PendingIntent.GetService(this, 1, play, pendingIntentFlags);
		actionPlay ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_play, ACTION_PLAY, pPlay).Build();

		var previous = new Intent(this, typeof(MediaControlsService));
		previous.SetAction("MediaAction.rewind");
		var pPrevious = PendingIntent.GetService(this, 1, previous, pendingIntentFlags);
		actionPrevious ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_rewind, ACTION_REWIND, pPrevious).Build();

		var next = new Intent(this, typeof(MediaControlsService));
		next.SetAction("MediaAction.fastForward");
		var pNext = PendingIntent.GetService(this, 1, next, pendingIntentFlags);
		actionNext ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_fastforward, ACTION_FASTFORWARD, pNext).Build();

		notification?.AddAction(actionPrevious);
		notification?.AddAction(actionPause);
		notification?.AddAction(actionNext);
	}

	static void BroadcastUpdate(string receiver, string action)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			System.Diagnostics.Trace.WriteLine($"{LocalBroadcastManager.GetInstance} not supported on Android 13 and above.");
			return;
		}
		var intent = new Intent(receiver);
		intent.PutExtra("ACTION", action);
		LocalBroadcastManager.GetInstance(Platform.AppContext).SendBroadcast(intent);
	}

	protected override void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				safeHandle?.Dispose();
				safeHandle = null;
			}
			audioManager?.AbandonAudioFocus(null);
			audioManager?.SetParameters("Ducking=false");
			audioManager?.Dispose();
			mediaSession?.Release();
			mediaSession?.Dispose();
			mediaSession = null;

			if (receiveUpdates is not null)
			{
				receiveUpdates.PropertyChanged -= OnReceiveUpdatesPropertyChanged;
				LocalBroadcastManager.GetInstance(Platform.AppContext).UnregisterReceiver(receiveUpdates);
			}
			receiveUpdates?.Dispose();
			receiveUpdates = null;
			isDisposed = true;
		}
		base.Dispose(disposing);
	}
}

/// <summary>
/// A <see cref="BroadcastReceiver"/> that listens for updates from the <see cref="MediaManager"/>. 
/// </summary>
sealed class ReceiveUpdates : BroadcastReceiver
{
	public string Action = string.Empty;

	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Method that is called when a broadcast is received.
	/// </summary>
	/// <param name="context"></param>
	/// <param name="intent"></param>
	public override void OnReceive(Context? context, Intent? intent)
	{
		ArgumentNullException.ThrowIfNull(intent);
		ArgumentNullException.ThrowIfNull(intent.Action);
		Action = intent.GetStringExtra("ACTION") ?? string.Empty;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Action)));
	}
}