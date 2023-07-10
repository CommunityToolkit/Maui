namespace CommunityToolkit.Maui.ApplicationModel;

using UIKit;
using UserNotifications;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, e) =>
		{
		});
		UIApplication.SharedApplication.ApplicationIconBadgeNumber = count;
	}

	/// <inheritdoc />
	public int GetCount()
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, e) =>
		{
		});
		return (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber;
	}
}