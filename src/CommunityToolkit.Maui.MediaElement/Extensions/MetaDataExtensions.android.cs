using Android.Content;
using Resource = Microsoft.Maui.Resource;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Android.Graphics;
using Android.Content.Res;
using Android.App;

namespace CommunityToolkit.Maui.Extensions;

public partial class MetaDataExtensions
{
	public static AndroidX.Core.App.NotificationCompat.Builder SetNotifications(Context context, string nOTIFICATION_CHANNEL_ID, MediaSessionCompat.Token token, string title, string artist, string album, Bitmap? bitmap, PendingIntent? pendingIntent)
	{
		var style = new AndroidX.Media.App.NotificationCompat.MediaStyle();
		style.SetMediaSession(token);
		style.SetShowActionsInCompactView(0, 1, 2);
		var notification = new AndroidX.Core.App.NotificationCompat.Builder(context, nOTIFICATION_CHANNEL_ID);
		notification.SetStyle(style); 
		notification.SetContentTitle(title);
		notification.SetContentText(artist);
		notification.SetAllowSystemGeneratedContextualActions(true);
		notification.SetContentIntent(pendingIntent);
		notification.SetSubText(album);
		notification.SetLargeIcon(bitmap);
		notification.SetSmallIcon(Resource.Drawable.exo_ic_audiotrack);
		notification.SetColor(AndroidX.Core.Content.ContextCompat.GetColor(Platform.AppContext, Resource.Color.notification_icon_bg_color));
		notification.SetOnlyAlertOnce(true);
		notification.SetVisibility(AndroidX.Core.App.NotificationCompat.VisibilityPublic);
		return notification;
	}

	public static Android.Support.V4.Media.MediaMetadataCompat.Builder SetMetadata(string album, string artist,string title, Bitmap? bitmap)
	{
		var metadataBuilder = new MediaMetadataCompat.Builder();
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyAlbum, album);
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyArtist, artist);
		metadataBuilder.PutString(MediaMetadataCompat.MetadataKeyTitle, title);
		metadataBuilder.PutBitmap(MediaMetadataCompat.MetadataKeyAlbumArt, bitmap);
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
