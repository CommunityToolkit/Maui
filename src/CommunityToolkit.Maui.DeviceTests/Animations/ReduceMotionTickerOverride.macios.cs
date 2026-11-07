using System.Reflection;
using Microsoft.Maui.Animations;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Switches MAUI's animation ticker on for this process only while the system's Reduce Motion setting is on.
/// </summary>
sealed class ReduceMotionTickerOverride
{
	// MAUI 11's PlatformTicker sets SystemEnabled from UIAccessibility.IsReduceMotionEnabled, and MAUI then jumps to the end of every animation
	// (CI's macOS hosts run with Reduce Motion on). The setter is protected and MAUI offers no public switch, so it is invoked through
	// reflection: that changes only this process's ticker, never the user's accessibility setting, and nothing outlives the test process.
	// typeof(Ticker) with a literal name and binding flags is a pattern the trimmer and NativeAOT analyzers understand, so the setter is kept.
	static readonly MethodInfo? systemEnabledSetter = typeof(Ticker)
		.GetProperty(nameof(ITicker.SystemEnabled), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?
		.GetSetMethod(nonPublic: true);

	ITicker? tickerToRestore;

	/// <returns>The reason the ticker could not be switched on, or <see langword="null"/> when it sees animations enabled.</returns>
	public string? TryEnable(IPlatformViewHandler handler)
	{
		if (handler.MauiContext?.Services.GetService(typeof(ITicker)) is not ITicker ticker || ticker.SystemEnabled)
		{
			return null;
		}

		if (ticker is not Ticker || systemEnabledSetter is null)
		{
			return $"MAUI's ticker ({ticker.GetType().FullName}) has no SystemEnabled setter that can be reached.";
		}

		try
		{
			systemEnabledSetter.Invoke(ticker, [true]);
		}
		catch (TargetInvocationException ex)
		{
			return $"{ex.InnerException?.GetType().Name}: {ex.InnerException?.Message}";
		}

		tickerToRestore = ticker;
		return null;
	}

	public void Restore()
	{
		if (tickerToRestore is null)
		{
			return;
		}

		// Each test disposes its own MauiApp and ticker, so this only matters if a ticker ever outlives its test.
		// Reduce Motion may have been switched off in the meantime: PlatformTicker then already re-enabled the ticker itself, and putting back
		// the old value would turn animations off against the real setting.
		if (UIKit.UIAccessibility.IsReduceMotionEnabled)
		{
			systemEnabledSetter?.Invoke(tickerToRestore, [false]);
		}

		tickerToRestore = null;
	}
}