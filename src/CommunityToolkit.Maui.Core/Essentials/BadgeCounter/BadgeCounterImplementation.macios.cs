namespace CommunityToolkit.Maui.BadgeCounter;

using UIKit;
using UserNotifications;

/// <inheritdoc />
public class BadgeCounterImplementation : IBadgeCounter
{
	/// <inheritdoc />
	public void SetBadgeCount(int count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, e) =>
		{
		});
		UIApplication.SharedApplication.ApplicationIconBadgeNumber = count;
	}
}