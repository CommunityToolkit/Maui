using System.Runtime.InteropServices;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Support.V4.Media.Session;
using Microsoft.Win32.SafeHandles;
using Resource = Microsoft.Maui.Resource;
using Stream = Android.Media.Stream;

namespace CommunityToolkit.Maui.Services;

[Service(Exported = true, Name ="CommunityToolkit.Maui.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service	
{
	bool disposedValue;

	SafeHandle? safeHandle = new SafeFileHandle(IntPtr.Zero, true);

	Bitmap? bitmap = null;
	PlaybackStateCompat.Builder? stateBuilder;
	MediaSessionCompat? mediaSession;
	AudioManager? audioManager;

	public MediaControlsService()
	{
	}

	public void startForegroundServiceAsync(MediaSessionCompat.Token token)
	{
		mediaSession = new MediaSessionCompat(this, "notification")
		{
			Active = true
		};
		
		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
			? PendingIntentFlags.UpdateCurrent |
			  PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);
		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		audioManager = GetSystemService(Context.AudioService) as AudioManager;

		audioManager?.RequestAudioFocus(null, Stream.Music, AudioFocus.Gain);
		audioManager?.SetParameters("Ducking=true");
		audioManager?.SetStreamVolume(Stream.Music, audioManager.GetStreamVolume(Stream.Music), VolumeNotificationFlags.ShowUi);
		
		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);
		style.SetShowActionsInCompactView(0,1,2);
		
		var notification = new AndroidX.Core.App.NotificationCompat.Builder(Platform.AppContext, "1");
		notification.SetSmallIcon(Resource.Drawable.abc_control_background_material);
		notification.SetStyle(style);
		
		stateBuilder = new PlaybackStateCompat.Builder();
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
		var token = intent?.GetParcelableExtra("token") as MediaSessionCompat.Token;
		if (token is not null)
		{
			startForegroundServiceAsync(token);
		}
		
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
			audioManager?.AbandonAudioFocus(null);
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
