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

		var albumArtUri = mediaManagerIntent?.GetStringExtra("albumArtUri") as string ?? string.Empty;
		var bitmap = await MediaManager.GetBitmapFromUrl(albumArtUri, Platform.AppContext.Resources);
		var title = mediaManagerIntent?.GetStringExtra("title") as string ?? string.Empty;
		var artist = mediaManagerIntent?.GetStringExtra("artist") as string ?? string.Empty;
		var album = mediaManagerIntent?.GetStringExtra("album") as string ?? string.Empty;

		var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
		? PendingIntentFlags.UpdateCurrent |
		  PendingIntentFlags.Mutable
		: PendingIntentFlags.UpdateCurrent;

		audioManager = GetSystemService(Context.AudioService) as AudioManager;
		audioManager?.RequestAudioFocus(null, Stream.Music, AudioFocus.Gain);
		audioManager?.SetParameters("Ducking=true");
		audioManager?.SetStreamVolume(Stream.Music, audioManager.GetStreamVolume(Stream.Music), VolumeNotificationFlags.ShowUi);

		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);
		NotificationCompat.Builder? notification = new AndroidX.Core.App.NotificationCompat.Builder(Platform.AppContext, "1");

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
			Intent pause = new Intent(this, typeof(MediaControlsService));
			pause.SetAction("MediaAction.pause");
			PendingIntent? pStop = PendingIntent.GetService(this, 1, pause, PendingIntentFlags.UpdateCurrent);
			NotificationCompat.Action action = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_pause, "MediaAction.pause", pStop).Build();

			Intent play = new Intent(this, typeof(MediaControlsService));
			play.SetAction("MediaAction.play");
			PendingIntent? pPlay = PendingIntent.GetService(this, 1, play, PendingIntentFlags.UpdateCurrent);
			NotificationCompat.Action actions = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_play, "MediaAction.play", pPlay).Build();

			Intent Previous = new Intent(this, typeof(MediaControlsService));
			Previous.SetAction("MediaAction.previous");
			PendingIntent? pPrevious = PendingIntent.GetService(this, 1, Previous, PendingIntentFlags.UpdateCurrent);
			NotificationCompat.Action actionPrevious = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_previous, "MediaAction.previous", pPrevious).Build();

			Intent Next = new Intent(this, typeof(MediaControlsService));
			Next.SetAction("MediaAction.next");
			PendingIntent? pNext = PendingIntent.GetService(this, 1, Next, PendingIntentFlags.UpdateCurrent);
			NotificationCompat.Action actionNext = new NotificationCompat.Action.Builder(Resource.Drawable.exo_controls_next, "MediaAction.next", pNext).Build();

			notification.AddAction(actionPrevious);
			notification.AddAction(action);
			notification.AddAction(actions);
			notification.AddAction(actionNext);

			notification.SetContentTitle(title);
			notification.SetContentText(artist);
			notification.SetSubText(album);
			notification.SetLargeIcon(bitmap);
		}
		
		notification.SetAutoCancel(false);
		notification.SetVisibility(NotificationCompat.VisibilityPublic);
		mediaSession?.SetSessionActivity(pendingIntent);
		mediaSession?.SetExtras(intent?.Extras);
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
		if ("MediaAction.pause".Equals(intent?.Action) && Platform.CurrentActivity is not null)
		{
			KeyEvent? keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPause);
			MediaManager.MediaControllerCompat?.DispatchMediaButtonEvent(keyEvent);
		}
		else if ("MediaAction.play".Equals(intent?.Action) && Platform.CurrentActivity is not null)
		{
			KeyEvent? keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPlay);
			MediaManager.MediaControllerCompat?.DispatchMediaButtonEvent(keyEvent);
		}
		else if ("MediaAction.previous".Equals(intent?.Action) && Platform.CurrentActivity is not null)
		{
			KeyEvent? keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaPrevious);
			MediaManager.MediaControllerCompat?.DispatchMediaButtonEvent(keyEvent);
		}
		else if ("MediaAction.next".Equals(intent?.Action) && Platform.CurrentActivity is not null)
		{
			KeyEvent? keyEvent = new KeyEvent(KeyEventActions.Down, Keycode.MediaNext);
			MediaManager.MediaControllerCompat?.DispatchMediaButtonEvent(keyEvent);
		}
		_ = startForegroundServiceAsync(intent);
		return StartCommandResult.NotSticky;
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