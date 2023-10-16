using System.Diagnostics;
using UserNotifications;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (r, error) =>
		{
			if (error is not null)
			{
				Trace.WriteLine($"Error Requesting Authorization to Set Badge Count: {error.Description}");
			}
		});

		UIApplication.SharedApplication.ApplicationIconBadgeNumber = (int)count;
	}
}