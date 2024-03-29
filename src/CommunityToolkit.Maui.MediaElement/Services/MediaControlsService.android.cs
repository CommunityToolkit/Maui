using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Media.App;
using Com.Google.Android.Exoplayer2;
using Microsoft.Maui.Controls.PlatformConfiguration;
using static Java.Util.Jar.Attributes;
using Resource = Microsoft.Maui.Resource;
using Com.Google.Android.Exoplayer2.Ext.Mediasession;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using static Android.Text.Style.TtsSpan;
using CommunityToolkit.Maui.Core.Views;
using static Android.Icu.Text.CaseMap;
using static Android.Provider.MediaStore.Audio;

namespace CommunityToolkit.Maui.Services;

[Service(Exported = true, Name ="CommunityToolkit.Maui.Services", ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class MediaControlsService : Service	
{
	readonly string nOTIFICATION_CHANNEL_ID = "1000";
	readonly int nOTIFICATION_ID = 1;
	readonly string nOTIFICATION_CHANNEL_NAME = "notification";

	MediaMetadataCompat? mediaMetadata;
	MediaSessionCompat? mediaSession;
	public MediaControlsService()
	{
	}
	public void startForegroundService(MediaSessionCompat.Token token,string title,string artist,string album,string albumArtUri,long duration)
	{
		mediaSession = new MediaSessionCompat(this, "notification");
		mediaSession.Active = true;
		
		var intent = new Intent(this, typeof(MediaManager));
		var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S
			? PendingIntentFlags.UpdateCurrent |
			  PendingIntentFlags.Immutable
			: PendingIntentFlags.UpdateCurrent;
		
		var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);
		var notificationManager = GetSystemService(Context.NotificationService) as NotificationManager;
		mediaSession.SetSessionActivity(pendingIntent);
		
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O && notificationManager is not null)
		{
			createNotificationChannel(notificationManager);
		}
		
		var metadataBuilder = new MediaMetadataCompat.Builder();
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyAlbum, album);
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyArtist, artist);
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyTitle, title);
		if (duration > 0)
		{
			metadataBuilder.PutLong(MediaMetadataCompat.MetadataKeyDuration, duration);
		}
		mediaSession.SetMetadata(metadataBuilder.Build());
		
		var notification = new AndroidX.Core.App.NotificationCompat.Builder(this, nOTIFICATION_CHANNEL_ID);
		notification.SetStyle(new AndroidX.Media.App.NotificationCompat.MediaStyle()
			.SetMediaSession(token));
			
		//notification.SetContentIntent(pendingIntent);
		notification.SetContentTitle(title);
		notification.SetContentText(artist);
		notification.SetSubText(album);
		notification.SetLargeIcon(Android.Graphics.BitmapFactory.DecodeResource(Resources, Resource.Drawable.exo_ic_audiotrack));
		notification.SetColor(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.notification_icon_bg_color));
		notification.SetSmallIcon(Resource.Drawable.exo_ic_audiotrack);
		notification.SetOnlyAlertOnce(true);
		notification.SetVisibility(AndroidX.Core.App.NotificationCompat.VisibilityPublic);

		System.Diagnostics.Debug.WriteLine("Notification created");
		if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
		{
			StartForeground(nOTIFICATION_ID, notification.Build(), ForegroundService.TypeMediaPlayback);
			return;
		}
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			StartForeground(nOTIFICATION_ID, notification.Build());
		}
	}

	void createNotificationChannel(NotificationManager notificationMnaManager)
	{
		var channel = new NotificationChannel(nOTIFICATION_CHANNEL_ID, nOTIFICATION_CHANNEL_NAME,
		NotificationImportance.Low);
		notificationMnaManager.CreateNotificationChannel(channel);
	}

	public override IBinder? OnBind(Intent? intent)
	{
		return null;
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		var token = intent?.GetParcelableExtra("token") as MediaSessionCompat.Token;
		var title = intent?.GetStringExtra("title") as string ?? string.Empty;
		var artist = intent?.GetStringExtra("artist") as string ?? string.Empty;
		var album = intent?.GetStringExtra("album") as string ?? string.Empty;
		var albumArtUri = intent?.GetStringExtra("albumArtUri") as string ?? string.Empty;
		var duration = intent?.GetLongExtra("duration", 0) ?? 0;

		if (token is not null)
		{
			startForegroundService(token, title, artist, album, albumArtUri, duration);
		}
		return StartCommandResult.NotSticky;
	}
}
