using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.ApplicationModel;
using AndroidApplication = Android.App.Application;

namespace CommunityToolkit.Maui.DeviceTests;

// Disabled in the manifest, so that by default no receiver is registered for the badge action, as on a launcher without badge support.
// A test enables it because DefaultBadgeProvider only broadcasts when the package manager lists a receiver. It does nothing itself, because
// Android 8 and later do not deliver an implicit broadcast to a manifest receiver: FilesBadgeBroadcastCapture observes the broadcast instead.
[BroadcastReceiver(Name = FilesBadgeReceiver.JavaName, Enabled = false, Exported = false)]
[IntentFilter([FilesBadgeReceiver.BadgeCountAction])]
public sealed class FilesBadgeReceiver : BroadcastReceiver
{
	public const string JavaName = "com.communitytoolkit.maui.devicetests.files.FilesBadgeReceiver";
	public const string BadgeCountAction = "android.intent.action.BADGE_COUNT_UPDATE";

	public override void OnReceive(Context? context, Intent? intent)
	{
	}
}

public static class FilesBadgeComponents
{
	public static IDisposable EnableBadgeReceiver() => new ComponentStateScope(new ComponentName(AndroidApplication.Context, FilesBadgeReceiver.JavaName), ComponentEnabledState.Enabled);

	// Leaves the package without a launch intent. The headless runner starts its own TestActivity and does not need MainActivity.
	public static IDisposable DisableLauncherActivity() => new ComponentStateScope(new ComponentName(AndroidApplication.Context, Java.Lang.Class.FromType(typeof(MainActivity))), ComponentEnabledState.Disabled);

	public static void RestoreManifestState()
	{
		var packageManager = AndroidApplication.Context.PackageManager ?? throw new InvalidOperationException("The app has no PackageManager.");
		foreach (var component in new[] { new ComponentName(AndroidApplication.Context, FilesBadgeReceiver.JavaName), new ComponentName(AndroidApplication.Context, Java.Lang.Class.FromType(typeof(MainActivity))) })
		{
			packageManager.SetComponentEnabledSetting(component, ComponentEnabledState.Default, ComponentEnableOption.DontKillApp);
		}
	}

	sealed class ComponentStateScope : IDisposable
	{
		readonly ComponentName component;

		public ComponentStateScope(ComponentName component, ComponentEnabledState state)
		{
			this.component = component;
			var packageManager = AndroidApplication.Context.PackageManager ?? throw new InvalidOperationException("The app has no PackageManager.");
			packageManager.SetComponentEnabledSetting(component, state, ComponentEnableOption.DontKillApp);
		}

		// Resets to the manifest's state (Default), not the state from before the scope, which a run killed halfway may have left changed.
		public void Dispose() =>
			AndroidApplication.Context.PackageManager?.SetComponentEnabledSetting(component, ComponentEnabledState.Default, ComponentEnableOption.DontKillApp);
	}
}

public sealed class FilesBadgeBroadcastCapture : BroadcastReceiver
{
	public const string SentinelAction = "files.test.BADGE_SENTINEL";

	readonly Lock gate = new();
	readonly List<Intent> badges = [];
	TaskCompletionSource? sentinel;
	bool registered;

	public FilesBadgeBroadcastCapture()
	{
		var filter = new IntentFilter(FilesBadgeReceiver.BadgeCountAction);
		filter.AddAction(SentinelAction);
		// Exported, because on Android 16 a receiver that is not exported gets nothing from the app's own implicit broadcasts.
		ContextCompat.RegisterReceiver(AndroidApplication.Context, this, filter, ContextCompat.ReceiverExported);
		registered = true;
	}

	public IReadOnlyList<Intent> Badges
	{
		get
		{
			lock (gate)
			{
				return [.. badges];
			}
		}
	}

	public override void OnReceive(Context? context, Intent? intent)
	{
		if (intent is null)
		{
			return;
		}

		if (intent.Action == SentinelAction)
		{
			lock (gate)
			{
				sentinel?.TrySetResult();
			}

			return;
		}

		lock (gate)
		{
			badges.Add(intent);
		}
	}

	// Broadcasts from one sender reach one receiver in the order they were sent, so a sentinel that comes back means that every earlier
	// broadcast has been delivered too.
	public async Task SettleAsync()
	{
		TaskCompletionSource arrived = new(TaskCreationOptions.RunContinuationsAsynchronously);
		lock (gate)
		{
			sentinel = arrived;
		}

		AndroidApplication.Context.SendBroadcast(new Intent(SentinelAction));
		await arrived.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}

	public void Stop()
	{
		if (registered)
		{
			registered = false;
			AndroidApplication.Context.UnregisterReceiver(this);
		}
	}
}