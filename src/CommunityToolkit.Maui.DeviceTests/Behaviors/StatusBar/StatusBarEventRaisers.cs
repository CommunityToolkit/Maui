using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Maui.Devices;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// .NET MAUI raises the events that StatusBarBehavior listens to from internal code, while a page is navigated to or the display changes.
/// Neither happens in the headless runners (no MAUI window, no navigation, no rotation), so the tests raise them by reflection.
/// </summary>
static class StatusBarEventRaisers
{
	const BindingFlags anyInstanceMember = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	static int displayInfoChanges;

	public static void RaiseNavigatedTo(Page page)
	{
		// MAUI ignores a second NavigatedTo until the page was navigated from.
		Invoke(page, "SendNavigatedFrom", Create<NavigatedFromEventArgs>(null, NavigationType.Push), false);
		Invoke(page, "SendNavigatedTo", CreateNavigatedToEventArgs());
	}

	public static NavigatedToEventArgs CreateNavigatedToEventArgs() => Create<NavigatedToEventArgs>(null, NavigationType.Push);

	// The metrics are made up because MAUI only raises MainDisplayInfoChanged when they differ from the last ones. Those last metrics are put
	// back afterwards: otherwise the platform's next real check would report a change back to the real metrics to whoever still listens,
	// later in the app run.
	public static void RaiseMainDisplayInfoChanged()
	{
		var display = DeviceDisplay.Current;
		var implementationBase = typeof(DeviceDisplay).Assembly.GetType("Microsoft.Maui.Devices.DeviceDisplayImplementationBase")
			?? throw new InvalidOperationException("MAUI no longer has Microsoft.Maui.Devices.DeviceDisplayImplementationBase.");
		var raise = implementationBase.GetMethod("OnMainDisplayInfoChanged", anyInstanceMember, [typeof(DisplayInfoChangedEventArgs)])
			?? throw new InvalidOperationException("MAUI's DeviceDisplay no longer has OnMainDisplayInfoChanged(DisplayInfoChangedEventArgs).");
		var lastMetrics = implementationBase.GetField("_currentMetrics", anyInstanceMember)
			?? throw new InvalidOperationException("MAUI's DeviceDisplay no longer keeps the metrics of the last change in a '_currentMetrics' field.");

		var current = display.MainDisplayInfo;
		var changed = new DisplayInfo(current.Width + ++displayInfoChanges, current.Height, current.Density, current.Orientation, current.Rotation, current.RefreshRate);
		var metricsBeforeTheChange = lastMetrics.GetValue(display);

		try
		{
			Invoke(raise, display, new DisplayInfoChangedEventArgs(changed));
		}
		finally
		{
			lastMetrics.SetValue(display, metricsBeforeTheChange);
		}
	}

	// For the private members of StatusBarBehavior that no public path reaches, such as a handler whose argument checks or early returns guard
	// against states MAUI never produces.
	public static void InvokeBehaviorMember(object? target, string methodName, params object?[] arguments)
	{
		var flags = (target is null ? BindingFlags.Static : BindingFlags.Instance) | BindingFlags.NonPublic;
		var method = typeof(CommunityToolkit.Maui.Behaviors.StatusBarBehavior).GetMethod(methodName, flags)
			?? throw new InvalidOperationException($"StatusBarBehavior no longer has a private {(target is null ? "static" : "instance")} method {methodName}.");

		Invoke(method, target!, arguments);
	}

	static void Invoke(Page page, string methodName, params object?[] arguments) =>
		Invoke(typeof(Page).GetMethod(methodName, anyInstanceMember) ?? throw new InvalidOperationException($"MAUI's Page no longer has {methodName}."), page, arguments);

	// Reflection wraps what the called code throws; the tests assert on the original exception.
	static void Invoke(MethodInfo method, object target, params object?[] arguments)
	{
		try
		{
			method.Invoke(target, arguments);
		}
		catch (TargetInvocationException exception) when (exception.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
		}
	}

	static T Create<T>(params object?[] arguments) where T : class
	{
		try
		{
			return (T)Activator.CreateInstance(typeof(T), anyInstanceMember, null, arguments, null)!;
		}
		catch (MissingMethodException)
		{
			var constructors = string.Join("; ", typeof(T).GetConstructors(anyInstanceMember).Select(static constructor => constructor.ToString()));
			throw new InvalidOperationException($"MAUI's {typeof(T).Name} has no constructor for ({string.Join(", ", arguments.Select(static argument => argument?.GetType().Name ?? "null"))}); it has: {constructors}");
		}
	}
}