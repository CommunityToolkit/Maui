using System.Diagnostics;
using UserNotifications;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(int count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, error) =>
		{
			if (error is not null)
			{
				Debug.WriteLine($"Error Requesting Authorization to Set Badge Count: {error.Description}");
			}
		});

		UIApplication.SharedApplication.ApplicationIconBadgeNumber = count;
	}

	/// <inheritdoc />
	public int GetCount()
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, error) =>
		{
			if (error is not null)
			{
				Debug.WriteLine($"Error Requesting Authorization to Get Badge Count: {error.Description}");
			}
		});

		return (int)UIApplication.SharedApplication.ApplicationIconBadgeNumber;
	}
}