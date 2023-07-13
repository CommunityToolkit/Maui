using System.Diagnostics;
using UserNotifications;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, e) =>
		{
			Debug.WriteLine($"Error Requesting Authorization to Set Badge Count: {e.Description}");
		});
		UIApplication.SharedApplication.ApplicationIconBadgeNumber = count;
	}

	/// <inheritdoc />
	public int GetCount()
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, e) =>
		{
			Debug.WriteLine($"Error Requesting Authorization to Get Badge Count: {e.Description}");
		});
		return (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber;
	}
}