using System.ComponentModel;
using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Support.V4.Media.Session;
using Android.Views;
using AndroidX.Core.App;
using AndroidX.LocalBroadcastManager.Content;
using Com.Google.Android.Exoplayer2;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Win32.SafeHandles;
using Resource = Microsoft.Maui.Resource;
using Stream = Android.Media.Stream;

namespace CommunityToolkit.Maui.Services;

[Service(Exported = true,Enabled = true, Name = "CommunityToolkit.Maui.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service	
{
	bool disposedValue;
	public const string ACTION_PLAY = "MediaAction.play";
	public const string ACTION_PAUSE = "MediaAction.pause";
	public const string ACTION_FORWARD = "MediaAction.advance_forward_30";
	public const string ACTION_BACK = "MediaAction.advance_backward_10";
	public const string ACTION_UPDATE_UI = "CommunityToolkit.Maui.Services.action.UPDATE_UI";
	bool notificationExists = false;

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

	public MediaControlsService()
	{
	}

	public async Task startForegroundServiceAsync(Intent mediaManagerIntent)
	{
		ArgumentNullException.ThrowIfNull(mediaManagerIntent);
		token ??= mediaManagerIntent.GetParcelableExtra("token") as MediaSessionCompat.Token;
		ArgumentNullException.ThrowIfNull(token);
		
		mediaSession ??= new MediaSessionCompat(Platform.AppContext, "notification")
		{
			Active = true,
		};
		ArgumentNullException.ThrowIfNull(mediaSession);
		receiveUpdates ??= new ReceiveUpdates();
		LocalBroadcastManager.GetInstance(this).RegisterReceiver(receiveUpdates, new IntentFilter(MediaControlsService.ACTION_UPDATE_UI));
		receiveUpdates.PropertyChanged += ReceiveUpdates_PropertyChanged;

		OnSetupAudioServices();

		var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
		? PendingIntentFlags.UpdateCurrent |
		  PendingIntentFlags.Immutable
		: PendingIntentFlags.UpdateCurrent;

		if (notification is not null)
		{
			notificationExists = true;
		}
		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);
		notification ??= new NotificationCompat.Builder(Platform.AppContext, "1");

		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);
		if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
		{
			style.SetShowActionsInCompactView(1,2,3);
		}

		notification.SetStyle(style);
		notification.SetSmallIcon(Resource.Drawable.exo_styled_controls_audiotrack);

		if (Build.VERSION.SdkInt < BuildVersionCodes.Tiramisu)
		{
			OnSetIntents();
			await OnSetContent(mediaManagerIntent);
		}
		
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

	void ReceiveUpdates_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			return;
		}
		
		if (e.PropertyName == "Action")
		{
			var action = receiveUpdates?.GetAction();
			switch (action)
			{
				case "MediaAction.pause":
					HandleNotificationEvent(false);
					break;
				case "MediaAction.play":
					HandleNotificationEvent(true);
					break;
			}
		}
	}

	static void CreateNotificationChannel(NotificationManager notificationMnaManager)
	{
		var channel = new NotificationChannel("1", "notification", NotificationImportance.Low);
		notificationMnaManager.CreateNotificationChannel(channel);
	}

	public override IBinder? OnBind(Intent? intent)
	{
		return null;
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		if (intent is null)
		{
			return StartCommandResult.NotSticky;
		}
		switch (intent.Action)
		{
			case "MediaAction.pause":
				HandleNotificationEvent(false);
				BroadcastUpdate(ACTION_PAUSE);
				break;
			case "MediaAction.play":
				HandleNotificationEvent(true);
				BroadcastUpdate(ACTION_PLAY);
				break;
			case "MediaAction.previous":
				HandleNotificationEvent(true);
				BroadcastUpdate(ACTION_BACK);
				break;
			case "MediaAction.next":
				HandleNotificationEvent(true);
				BroadcastUpdate(ACTION_FORWARD);
				break;
		}

		_ = startForegroundServiceAsync(intent);
		return StartCommandResult.NotSticky;
	}
	void HandleNotificationEvent(bool isPlaying)
	{
		if (notification is null)
		{
			return;
		}
		notification.ClearActions();
		notification.AddAction(actionPrevious);
		notification.AddAction(isPlaying ? actionPause : actionPlay);
		notification.AddAction(actionNext);
		notification.Build();
	}
	void OnSetupAudioServices()
	{
		audioManager = GetSystemService(Context.AudioService) as AudioManager;
		ArgumentNullException.ThrowIfNull(audioManager);
		audioManager.RequestAudioFocus(null, Stream.Music, AudioFocus.Gain);
		audioManager.SetParameters("Ducking=true");
		audioManager.SetStreamVolume(Stream.Music, audioManager.GetStreamVolume(Stream.Music), VolumeNotificationFlags.ShowUi);
	}

	async Task OnSetContent(Intent mediaManagerIntent)
	{
		var albumArtUri = mediaManagerIntent.GetStringExtra("albumArtUri") as string ?? string.Empty;
		var bitmap = await MediaManager.GetBitmapFromUrl(albumArtUri, Platform.AppContext.Resources);
		var title = mediaManagerIntent.GetStringExtra("title") as string ?? string.Empty;
		var artist = mediaManagerIntent.GetStringExtra("artist") as string ?? string.Empty;
		var album = mediaManagerIntent.GetStringExtra("album") as string ?? string.Empty;
		notification?.SetContentTitle(title);
		notification?.SetContentText(artist);
		notification?.SetSubText(album);
		notification?.SetLargeIcon(bitmap);
	}

	void OnSetIntents()
	{
		var pause = new Intent(this, typeof(MediaControlsService));
		pause.SetAction("MediaAction.pause");
		var pPause = PendingIntent.GetService(this, 1, pause, PendingIntentFlags.UpdateCurrent);
		actionPause ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_pause, ACTION_PAUSE, pPause).Build();

		var play = new Intent(this, typeof(MediaControlsService));
		play.SetAction("MediaAction.play");
		var pPlay = PendingIntent.GetService(this, 1, play, PendingIntentFlags.UpdateCurrent);
		actionPlay ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_play, ACTION_PLAY, pPlay).Build();

		var previous = new Intent(this, typeof(MediaControlsService));
		previous.SetAction("MediaAction.previous");
		var pPrevious = PendingIntent.GetService(this, 1, previous, PendingIntentFlags.UpdateCurrent);
		actionPrevious ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_rewind, ACTION_BACK, pPrevious).Build();

		var next = new Intent(this, typeof(MediaControlsService));
		next.SetAction("MediaAction.next");
		var pNext = PendingIntent.GetService(this, 1, next, PendingIntentFlags.UpdateCurrent);
		actionNext ??= new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_fastforward, ACTION_FORWARD, pNext).Build();
		if (!notificationExists)
		{
			notification?.AddAction(actionPrevious);
			notification?.AddAction(actionPause);
			notification?.AddAction(actionNext);
		}
	}
	void BroadcastUpdate(string action)
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
		{
			return;
		}
		var intent = new Intent(ACTION_UPDATE_UI);
		intent.PutExtra("ACTION", action);
		LocalBroadcastManager.GetInstance(this).SendBroadcast(intent);
	}
	protected override void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				safeHandle?.Dispose();
				safeHandle = null;
			}
			_ = (audioManager?.AbandonAudioFocus(null));
			audioManager?.SetParameters("Ducking=false");
			audioManager?.Dispose();
			mediaSession?.Release();
			mediaSession?.Dispose();
			mediaSession = null;
			
			if (receiveUpdates is not null)
			{
			
				receiveUpdates.PropertyChanged -= ReceiveUpdates_PropertyChanged;	LocalBroadcastManager.GetInstance(Platform.AppContext).UnregisterReceiver(receiveUpdates);
			}
			receiveUpdates?.Dispose();
			receiveUpdates = null;
			disposedValue = true;
		}
		base.Dispose(disposing);
	}
}

/// <summary>
/// A <see cref="BroadcastReceiver"/> that listens for updates from the <see cref="MediaManager"/>. 
/// </summary>
class ReceiveUpdates : BroadcastReceiver
{
	string action = string.Empty;
	public string Action
	{
		get => action;
		set
		{
			if (action != value)
			{
				action = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Action)));
			}
		
		}
	}
	public event PropertyChangedEventHandler? PropertyChanged;
	/// <summary>
	/// 
	/// </summary>
	/// <param name="context"></param>
	/// <param name="intent"></param>
	public override void OnReceive(Context? context, Intent? intent)
	{
		Action = intent?.GetStringExtra("ACTION") ?? string.Empty;
	}
	public string GetAction()
	{
		return Action ?? string.Empty;
	}
}