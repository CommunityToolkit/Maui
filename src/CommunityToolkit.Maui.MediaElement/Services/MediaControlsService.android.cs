using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Media;
using Android.OS;
using Android.Support.V4.Media.Session;
using Android.Views;
using AndroidX.Core.App;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Win32.SafeHandles;
using Resource = Microsoft.Maui.Resource;
using Stream = Android.Media.Stream;

namespace CommunityToolkit.Maui.Services;

[Service(Exported = true,Enabled = true, Name = "CommunityToolkit.Maui.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service	
{
	bool disposedValue;
	SafeHandle? safeHandle = new SafeFileHandle(IntPtr.Zero, true);
	PlaybackStateCompat.Builder? stateBuilder;
	MediaSessionCompat? mediaSession;
	AudioManager? audioManager;
	NotificationCompat.Builder? notification;
	PendingIntentFlags pendingIntentFlags;

	public MediaControlsService()
	{
	}

	public async Task startForegroundServiceAsync(Intent? mediaManagerIntent)
	{
		var token = mediaManagerIntent?.GetParcelableExtra("token") as MediaSessionCompat.Token;
		if (mediaSession is null && token is not null)
		{
			mediaSession = new MediaSessionCompat(Platform.AppContext, "notification")
			{
				Active = true,
			};
		}
		OnSetupAudioServices();

		pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
		? PendingIntentFlags.UpdateCurrent |
		  PendingIntentFlags.Immutable
		: PendingIntentFlags.UpdateCurrent;

		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);
		notification = new NotificationCompat.Builder(Platform.AppContext, "1");

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
		mediaSession?.SetSessionActivity(pendingIntent);
		mediaSession?.SetExtras(intent.Extras);
		mediaSession?.SetPlaybackToLocal(AudioManager.AudioSessionIdGenerate);
		mediaSession?.SetSessionActivity(pendingIntent);
				
		notification.Build();

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O && notificationManager is not null)
		{
			CreateNotificationChannel(notificationManager);
		}

		if (Build.VERSION.SdkInt >= BuildVersionCodes.Q && notification is not null)
		{
			StartForeground(1, notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}

		notificationManager?.Notify(1, notification?.Build());
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O && notification is not null)
		{
			StartForeground(1, notification.Build());
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
		KeyEvent? keyEvent = null;
		switch (intent.Action)
		{
			case "MediaAction.pause":
				keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPause);		
				break;
			case "MediaAction.play":
				keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPlay);
				break;
				case "MediaAction.previous":
				keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPrevious);
				break;
				case "MediaAction.next":
				keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaNext);
				break;
		}

		if (keyEvent is not null)
		{
			MediaManager.MediaControllerCompat?.DispatchMediaButtonEvent(keyEvent);
		}
		_ = startForegroundServiceAsync(intent);
		return StartCommandResult.NotSticky;
	}

	void OnSetupAudioServices()
	{
		audioManager = GetSystemService(Context.AudioService) as AudioManager;
		audioManager?.RequestAudioFocus(null, Stream.Music, AudioFocus.Gain);
		audioManager?.SetParameters("Ducking=true");
		audioManager?.SetStreamVolume(Stream.Music, audioManager.GetStreamVolume(Stream.Music), VolumeNotificationFlags.ShowUi);
	}

	async Task OnSetContent(Intent? mediaManagerIntent)
	{
		var albumArtUri = mediaManagerIntent?.GetStringExtra("albumArtUri") as string ?? string.Empty;
		var bitmap = await MediaManager.GetBitmapFromUrl(albumArtUri, Platform.AppContext.Resources);
		var title = mediaManagerIntent?.GetStringExtra("title") as string ?? string.Empty;
		var artist = mediaManagerIntent?.GetStringExtra("artist") as string ?? string.Empty;
		var album = mediaManagerIntent?.GetStringExtra("album") as string ?? string.Empty;
		notification?.SetContentTitle(title);
		notification?.SetContentText(artist);
		notification?.SetSubText(album);
		notification?.SetLargeIcon(bitmap);
	}

	void OnSetIntents()
	{
		Intent pause = new Intent(this, typeof(MediaControlsService));
		pause.SetAction("MediaAction.pause");
		PendingIntent? pPause = PendingIntent.GetService(this, 1, pause, pendingIntentFlags);
		NotificationCompat.Action actionPause = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_pause, "MediaAction.pause", pPause).Build();

		Intent play = new Intent(this, typeof(MediaControlsService));
		play.SetAction("MediaAction.play");
		PendingIntent? pPlay = PendingIntent.GetService(this, 1, play, pendingIntentFlags);
		NotificationCompat.Action actionPlay = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_play, "MediaAction.play", pPlay).Build();

		Intent Previous = new Intent(this, typeof(MediaControlsService));
		Previous.SetAction("MediaAction.previous");
		PendingIntent? pPrevious = PendingIntent.GetService(this, 1, Previous, pendingIntentFlags);
		NotificationCompat.Action actionPrevious = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_previous, "MediaAction.previous", pPrevious).Build();

		Intent Next = new Intent(this, typeof(MediaControlsService));
		Next.SetAction("MediaAction.next");
		PendingIntent? pNext = PendingIntent.GetService(this, 1, Next, pendingIntentFlags);
		NotificationCompat.Action actionNext = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_next, "MediaAction.next", pNext).Build();

		notification?.AddAction(actionPrevious);
		notification?.AddAction(actionPause);
		notification?.AddAction(actionPlay);
		notification?.AddAction(actionNext);
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
			mediaSession?.Dispose();
			mediaSession = null;
			stateBuilder?.Dispose();
			stateBuilder = null;
			disposedValue = true;
		}
		base.Dispose(disposing);
	}
}