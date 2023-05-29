namespace CommunityToolkit.Maui.ApplicationModel;

using Android.App;
using AndroidX.Core.App;

//public class BadgeImplementation : IBadge
//{
//	public void SetCount(int count)
//	{
//		var notificationManager = (NotificationManager)Application.Context.GetSystemService(Android.Content.Context.NotificationService);
//		var channel = new NotificationChannel("default", "Default", NotificationImportance.Default);
//		notificationManager.CreateNotificationChannel(channel);
//		var builder = new NotificationCompat.Builder(Application.Context, "default");
//		builder.SetNumber(count);
//		notificationManager.Notify(0, builder.Build());
//	}
//}

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		if (ME.Leolin.Shortcutbadger.ShortcutBadger.IsBadgeCounterSupported(Application.Context))
		{
			ME.Leolin.Shortcutbadger.ShortcutBadger.ApplyCount(Application.Context, count);
		}
	}
}