using System.Reflection;
using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.DeviceTests;

// Snackbar and Toast keep their platform alert, and Snackbar its IsShown flag, in static state, so a test that stops half way (or that
// ends while an alert is still visible) would leak into the next test: every test ends with CleanUpAlertsAsync (per platform).
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

	// Snackbar.PlatformSnackbar and Toast.PlatformToast are private statics: the tests read native state from the object, or from the views
	// it created.
	protected static object? GetPlatformSnackbar() => GetStaticProperty(typeof(Snackbar), "PlatformSnackbar");

	protected static object? GetPlatformToast() => GetStaticProperty(typeof(Toast), "PlatformToast");

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