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

		if (OperatingSystem.IsIOSVersionAtLeast(17))
		{
			UNUserNotificationCenter.Current.SetBadgeCount(new IntPtr((int)count), (error) =>
			{
				if (error is not null)
				{
					Trace.WriteLine($"Error setting the Badge Count: {error.Description}");
				}
			});
		}
		else
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = (int)count;
		}
	}
}