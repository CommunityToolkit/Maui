using System.Reflection;
using Android.App;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

// Lets the code that asks Platform.CurrentActivity for the activity (MauiMediaElement's full-screen handling) run in situations that a
// running app does not produce on demand: no current activity, or an activity that has no window. Only the UI thread may be using the
// activity meanwhile, so keep the scope short and call it on the UI thread.
public sealed class ActivityStateOverride : IDisposable
{
	// ActivityStateManager.SetDefault is internal; the field behind ActivityStateManager.Default is the only way to swap the implementation.
	static readonly FieldInfo defaultImplementationField = typeof(ActivityStateManager).GetField("defaultImplementation", BindingFlags.Static | BindingFlags.NonPublic)!;

	readonly IActivityStateManager original = ActivityStateManager.Default;

	public ActivityStateOverride(Activity? currentActivity) => defaultImplementationField.SetValue(null, new FixedActivityStateManager(currentActivity));

	public void Dispose() => defaultImplementationField.SetValue(null, original);

	sealed class FixedActivityStateManager(Activity? currentActivity) : IActivityStateManager
	{
		public event EventHandler<ActivityStateChangedEventArgs>? ActivityStateChanged
		{
			add
			{
			}
			remove
			{
			}
		}

		public Activity? GetCurrentActivity() => currentActivity;

		public void Init(Android.App.Application application)
		{
		}

		public void Init(Activity activity, Bundle? bundle)
		{
		}

		public Task<Activity> WaitForActivityAsync(CancellationToken cancelToken = default) =>
			currentActivity is null ? Task.FromCanceled<Activity>(new CancellationToken(canceled: true)) : Task.FromResult(currentActivity);
	}
}