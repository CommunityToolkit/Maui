using Android.Content;
using Resource = Microsoft.Maui.Resource;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Android.Graphics;
using Android.Content.Res;
using Android.App;
using Android.Media;

namespace CommunityToolkit.Maui.Extensions;

public partial class MetaDataExtensions
{
	public static AndroidX.Core.App.NotificationCompat.Builder SetNotifications(Context context, string nOTIFICATION_CHANNEL_ID, MediaSessionCompat.Token token, string title, string artist, string album, Bitmap? bitmap, PendingIntent? pendingIntent,long duration)
	{
		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);
		style.SetShowActionsInCompactView(0,1,2);
		style.SetShowCancelButton(true);
		var notification = new AndroidX.Core.App.NotificationCompat.Builder(context, nOTIFICATION_CHANNEL_ID);
		notification.SetStyle(style); 
		notification.SetContentTitle(title);
		notification.SetContentText(artist);
		notification.Extras.PutLong(MediaMetadata.MetadataKeyDuration, duration);
		notification.AddAction(Resource.Drawable.exo_controls_previous, "Previous", pendingIntent);
		notification.AddAction(Resource.Drawable.exo_controls_pause, "Pause", pendingIntent);
		notification.AddAction(Resource.Drawable.exo_controls_play, "Play", pendingIntent);
		notification.AddAction(Resource.Drawable.exo_controls_next, "Next", pendingIntent);
		notification.AddAction(Resource.Drawable.exo_controls_fastforward, "FastForward", pendingIntent);
		notification.AddAction(Resource.Drawable.exo_controls_rewind, "Rewind", pendingIntent);
		notification.SetAllowSystemGeneratedContextualActions(true);
		notification.SetContentIntent(pendingIntent);
		notification.SetSubText(album);
		notification.SetLargeIcon(bitmap);
		notification.SetSmallIcon(Resource.Drawable.exo_styled_controls_audiotrack);
		notification.SetColor(AndroidX.Core.Content.ContextCompat.GetColor(Platform.AppContext, Resource.Color.notification_icon_bg_color));
		notification.SetOnlyAlertOnce(true);
		notification.SetVisibility(AndroidX.Core.App.NotificationCompat.VisibilityPublic);
		return notification;
	}

	public static Android.Support.V4.Media.MediaMetadataCompat.Builder SetMetadata(string album, string artist,string title, Bitmap? bitmap, long duration, long position)
	{
		var metadataBuilder = new MediaMetadataCompat.Builder();
		metadataBuilder.PutString(MediaMetadata.MetadataKeyAlbumArtist, album);
		metadataBuilder.PutString(MediaMetadata.MetadataKeyArtist, artist);
		metadataBuilder.PutString(MediaMetadata.MetadataKeyTitle, title);
		metadataBuilder.PutLong(MediaMetadata.MetadataKeyDuration, duration);
		metadataBuilder.PutBitmap(MediaMetadata.MetadataKeyAlbumArt, bitmap);
		return metadataBuilder;
	}

	public static async Task<Bitmap?> GetBitmapFromUrl(string? url, Resources? resources)
	{
		var temp = BitmapFactory.DecodeResource(resources, Resource.Drawable.exo_ic_audiotrack);
		try
		{
			var client = new HttpClient();
			var response = await client.GetAsync(url);
			var stream = response.IsSuccessStatusCode ? await response.Content.ReadAsStreamAsync() : null;
			return stream is not null ? await BitmapFactory.DecodeStreamAsync(stream) : temp;
		}
		catch
		{
			return temp;
		}
	}
}
