using System.Reflection;
using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.DeviceTests;

// Base class of the Snackbar and Toast tests. Both toolkit types keep their platform alert, and Snackbar its IsShown flag, in static
// state, so a test that stops half way (or that ends while an alert is still visible) would leak into the next test. Every test
// therefore ends by dismissing what is visible, clearing the statics and restoring Application.Current (CleanUpAlertsAsync, per platform).
public abstract partial class AlertsTestBase : CommunityToolkitHandlerTestBase
{
	protected override async ValueTask DisposeAsyncCore()
	{
		try
		{
			await base.DisposeAsyncCore();
		}
		finally
		{
			await InvokeOnMainThreadAsync(CleanUpAlertsAsync);
		}
	}

	// The toolkit's platform alert of the last Show call (null when none): Snackbar.PlatformSnackbar and Toast.PlatformToast, which are
	// private statics. The tests read native state from the object, or from the views it created.
	protected static object? GetPlatformSnackbar() => GetStaticProperty(typeof(Snackbar), "PlatformSnackbar");

	protected static object? GetPlatformToast() => GetStaticProperty(typeof(Toast), "PlatformToast");

	// Puts Snackbar and Toast back into the state of a fresh process, so that "nothing is shown" is deterministic.
	protected static void ResetToolkitAlertState()
	{
		SetStaticProperty(typeof(Snackbar), "PlatformSnackbar", null);
		SetStaticProperty(typeof(Snackbar), nameof(Snackbar.IsShown), false);
		SetStaticProperty(typeof(Toast), "PlatformToast", null);
	}

	static object? GetStaticProperty(Type type, string name)
	{
		var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");
		return property.GetValue(null);
	}

	static void SetStaticProperty(Type type, string name, object? value)
	{
		var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException($"{type.FullName}.{name} was not found.");
		property.SetValue(null, value);
	}
}