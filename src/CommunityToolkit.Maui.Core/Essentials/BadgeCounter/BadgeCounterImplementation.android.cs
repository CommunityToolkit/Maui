namespace CommunityToolkit.Maui.BadgeCounter;

using Android.App;
using AndroidX.Core.App;

//public class BadgeCounterImplementation : IBadgeCounter
//{
//	public void SetBadgeCount(int count)
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
public class BadgeCounterImplementation : IBadgeCounter
{
	/// <inheritdoc />
	public void SetBadgeCount(int count)
	{
		if (ME.Leolin.Shortcutbadger.ShortcutBadger.IsBadgeCounterSupported(Application.Context))
		{
			ME.Leolin.Shortcutbadger.ShortcutBadger.ApplyCount(Application.Context, count);
		}
	}
}