using System.Diagnostics;
using UserNotifications;

namespace CommunityToolkit.Maui.ApplicationModel;

/// <inheritdoc />
public class BadgeImplementation : IBadge
{
	/// <inheritdoc />
	public void SetCount(uint count)
	{
		UNUserNotificationCenter.Current.RequestAuthorization(UNAuthorizationOptions.Badge, (_, error) => TraceAuthorizationError(error));

		// A cast to int would turn a count above int.MaxValue into a negative number, which the system rejects ("Badge count is invalid")
		var badgeCount = int.CreateSaturating(count);

		if (OperatingSystem.IsIOSVersionAtLeast(17))
		{
			UNUserNotificationCenter.Current.SetBadgeCount(badgeCount, TraceSetBadgeCountError);
		}
		else
		{
			UIApplication.SharedApplication.ApplicationIconBadgeNumber = badgeCount;
		}
	}

	// The answers of the system only reach the trace: the toolkit cannot do anything about a refusal
	internal static void TraceAuthorizationError(NSError? error)
	{
		if (error is not null)
		{
			Trace.WriteLine($"Error Requesting Authorization to Set Badge Count: {error.Description}");
		}
	}

	internal static void TraceSetBadgeCountError(NSError? error)
	{
		if (error is not null)
		{
			Trace.WriteLine($"Error setting the Badge Count: {error.Description}");
		}
	}
}