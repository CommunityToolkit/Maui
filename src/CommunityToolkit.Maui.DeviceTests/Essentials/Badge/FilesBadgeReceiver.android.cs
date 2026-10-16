using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.ApplicationModel;
using AndroidApplication = Android.App.Application;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// A manifest receiver for the broadcast that the toolkit's DefaultBadgeProvider sends. It is disabled in the manifest, so by default
/// no receiver is registered for the action, which is the state of a launcher without badge support. A test enables it with
/// <see cref="FilesBadgeComponents.EnableBadgeReceiver"/> to make the package manager list a receiver, which is what DefaultBadgeProvider
/// checks before it broadcasts. It does nothing itself: the broadcast is observed with <see cref="FilesBadgeBroadcastCapture"/>, because
/// Android 8 and later do not deliver an implicit broadcast to a manifest receiver.
/// </summary>
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

/// <summary>
/// Turns the app's own components on and off for a test. An app may change its own components, and
/// <see cref="ComponentEnabledState.Default"/> hands each one back to what the manifest says, so the state is restored even when a
/// previous run was killed halfway.
/// </summary>
public static class FilesBadgeComponents
{
	/// <summary>Enables the manifest receiver for the badge action until the returned object is disposed.</summary>
	public static IDisposable EnableBadgeReceiver() => new ComponentStateScope(new ComponentName(AndroidApplication.Context, FilesBadgeReceiver.JavaName), ComponentEnabledState.Enabled);

	/// <summary>
	/// Disables the app's launcher activity (the runner's MainActivity) until the returned object is disposed, so that the package has
	/// no launch intent. The headless runner starts its own TestActivity and does not need MainActivity.
	/// </summary>
	public static IDisposable DisableLauncherActivity() => new ComponentStateScope(new ComponentName(AndroidApplication.Context, Java.Lang.Class.FromType(typeof(MainActivity))), ComponentEnabledState.Disabled);

	/// <summary>Gives both components back to the manifest's state.</summary>
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

		public void Dispose() =>
			AndroidApplication.Context.PackageManager?.SetComponentEnabledSetting(component, ComponentEnabledState.Default, ComponentEnableOption.DontKillApp);
	}
}

/// <summary>
/// Collects the badge broadcasts that the app sends, for as long as it is registered. The receiver is registered as exported: on
/// Android 16 a not-exported receiver gets nothing from the app's own implicit broadcasts.
/// </summary>
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
		ContextCompat.RegisterReceiver(AndroidApplication.Context, this, filter, ContextCompat.ReceiverExported);
		registered = true;
	}

	/// <summary>The badge broadcasts received so far, in order.</summary>
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

	/// <summary>
	/// Waits until every broadcast that was sent before this call was delivered: broadcasts from one sender reach one receiver in the
	/// order they were sent, so a sentinel that comes back means that the earlier ones have.
	/// </summary>
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