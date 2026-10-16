using System.Reflection;
using Android.App;
using Android.OS;
using Microsoft.Maui.ApplicationModel;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Replaces the activity that <c>Platform.CurrentActivity</c> reports for as long as it is alive, then puts the original back. Code that
/// asks for the current activity (MauiMediaElement's full-screen handling) can then be run in the situations that a running app does not
/// produce on demand: no current activity, or an activity that has no window. Only the UI thread may be using the activity meanwhile, so
/// keep the scope short and call it on the UI thread.
/// </summary>
public sealed class ActivityStateOverride : IDisposable
{
	// ActivityStateManager.SetDefault is internal; the field behind ActivityStateManager.Default is the only way to swap the implementation.
	static readonly FieldInfo defaultImplementationField = typeof(ActivityStateManager).GetField("defaultImplementation", BindingFlags.Static | BindingFlags.NonPublic)!;

	readonly IActivityStateManager original = ActivityStateManager.Default;

	/// <param name="currentActivity">What <c>Platform.CurrentActivity</c> returns while the override is active.</param>
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