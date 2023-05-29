namespace CommunityToolkit.Maui.ApplicationModel;

using UIKit;
using UserNotifications;

/// <inheritdoc />
public class BadgeImplementation : IBadge
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