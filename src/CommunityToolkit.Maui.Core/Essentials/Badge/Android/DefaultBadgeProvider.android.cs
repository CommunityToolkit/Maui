using System.Diagnostics;
using Android.Content;
using Android.Content.PM;

namespace CommunityToolkit.Maui.ApplicationModel;

class DefaultBadgeProvider : IBadgeProvider
{
	const string intentAction = "android.intent.action.BADGE_COUNT_UPDATE";

	public void SetCount(uint count)
	{
		var packageName = Application.Context.PackageName;
		if (packageName is null)
		{
			Trace.WriteLine("Unable to get package name");
			return;
		}

		var component = Application.Context.PackageManager?.GetLaunchIntentForPackage(packageName)?.Component;
		if (component is null)
		{
			Trace.WriteLine($"Unable to get launch intent component for package {packageName}");
			return;
		}

		if (!CanSetBadgeCounter())
		{
			Trace.WriteLine("Current launcher doesn't support badge counter");
			return;
		}

		try
		{
			var intent = new Intent(intentAction);
			intent.PutExtra("badge_count_package_name", packageName);
			intent.PutExtra("badge_count_class_name", component.ClassName);
			intent.PutExtra("badge_count", count);
			Application.Context.SendBroadcast(intent);
		}
		catch (Exception ex)
		{
			Trace.WriteLine($"{nameof(DefaultBadgeProvider)} unable to set badge count: " + ex.Message);
		}
	}

	static bool CanSetBadgeCounter()
	{
		using var intent = new Intent(intentAction);
		var packageManager = Application.Context.PackageManager;
		var receivers = OperatingSystem.IsAndroidVersionAtLeast(33)
						? packageManager?.QueryBroadcastReceivers(intent, PackageManager.ResolveInfoFlags.Of(0))
						: packageManager?.QueryBroadcastReceivers(intent, 0);

		return receivers?.Count > 0;
	}
}