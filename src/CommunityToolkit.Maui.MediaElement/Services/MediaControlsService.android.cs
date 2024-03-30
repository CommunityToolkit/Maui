using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Support.V4.Media.Session;
using Android.Graphics;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Services;

[Service(Exported = true, Name ="CommunityToolkit.Maui.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service	
{
	Bitmap? bitmap = null;
	Android.Support.V4.Media.Session.PlaybackStateCompat.Builder? stateBuilder;
	MediaSessionCompat? mediaSession;

	public MediaControlsService()
	{
	}

	public async Task startForegroundServiceAsync(MediaSessionCompat.Token token,string title,string artist,string album,string albumArtUri, int position, int currentTime)
	{
		mediaSession = new MediaSessionCompat(this, "notification")
		{
			Active = true
		};
		await Task.Run(async () => bitmap = await MetaDataExtensions.GetBitmapFromUrl(albumArtUri, Resources));

		var intent = new Intent(this, typeof(MediaControlsService));
		var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
			? PendingIntentFlags.UpdateCurrent |
			  PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;
		var pendingIntent = PendingIntent.GetActivity(this, 2, intent, pendingIntentFlags);
		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		
		var notification = MetaDataExtensions.SetNotifications(Platform.AppContext, "1", token, title, artist, album, bitmap, pendingIntent);
		var metadataBuilder = MetaDataExtensions.SetMetadata(album, artist, title, bitmap);
		stateBuilder = new PlaybackStateCompat.Builder().SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionStop);
		stateBuilder?.SetState(PlaybackStateCompat.StatePlaying, position, 1.0f, currentTime);

		mediaSession?.SetMetadata(metadataBuilder.Build());
		mediaSession?.SetMetadata(metadataBuilder.Build());
		mediaSession?.SetPlaybackState(stateBuilder?.Build());

		mediaSession?.SetSessionActivity(pendingIntent);
		System.Diagnostics.Debug.WriteLine($"StateBuilder: current: {position} && current time is {currentTime}");

		if (Build.VERSION.SdkInt >= BuildVersionCodes.O && notificationManager is not null)
		{
			createNotificationChannel(notificationManager);
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

	void createNotificationChannel(NotificationManager notificationMnaManager)
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
		var position = intent?.GetIntExtra("position", 0) ?? 0;
		var currentTime = intent?.GetIntExtra("currentTime", 0) ?? 0;
		var title = intent?.GetStringExtra("title") as string ?? string.Empty;
		var artist = intent?.GetStringExtra("artist") as string ?? string.Empty;
		var album = intent?.GetStringExtra("album") as string ?? string.Empty;
		var albumArtUri = intent?.GetStringExtra("albumArtUri")  as string ?? string.Empty;

		if (token is not null)
		{
			_ = startForegroundServiceAsync(token, title, artist, album, albumArtUri, position, currentTime);
		}
		return StartCommandResult.NotSticky;
	}
}
