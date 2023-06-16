using System.Diagnostics;
using Android.Content;
using Android.Content.PM;

namespace CommunityToolkit.Maui.ApplicationModel;

class DefaultBadgeProvider : IBadgeProvider
{
	const string intentAction = "android.intent.action.BADGE_COUNT_UPDATE";

	public void SetCount(int count)
	{
		if (count < 0)
		{
			return;
		}

		var packageName = Application.Context.PackageName;
		if (packageName is null)
		{
			return;
		}

		var component = Application.Context.PackageManager?.GetLaunchIntentForPackage(packageName)?.Component;
		if (component is null)
		{
			return;
		}

		if (!IsSupported())
		{
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
			Debug.WriteLine("(DEFAULT) unable to set badge: " + ex.Message);
		}
	}
	

	bool IsSupported()
	{
		var intent = new Intent(intentAction);
		var packageManager = Application.Context.PackageManager;
		var receivers = OperatingSystem.IsAndroidVersionAtLeast(33) ?
			packageManager?.QueryBroadcastReceivers(intent, PackageManager.ResolveInfoFlags.Of(0)) :
			packageManager?.QueryBroadcastReceivers(intent, 0);
		return receivers is { Count: > 0 };
	}
}