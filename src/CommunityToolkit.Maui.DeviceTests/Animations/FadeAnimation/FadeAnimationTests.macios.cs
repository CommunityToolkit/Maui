using Microsoft.Maui.Animations;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	readonly SystemAnimationsTickerOverride reduceMotionOverride = new(static () => UIAccessibility.IsReduceMotionEnabled);

	static double GetNativeOpacity(IPlatformViewHandler handler) => ((UIView)(handler.ContainerView ?? handler.PlatformView!)).Alpha;

	// With Reduce Motion on, MAUI 11 jumps to the end of every animation, so no animation could start while another one runs.
	async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		string? error = null;
		await InvokeOnMainThreadAsync(() => error = reduceMotionOverride.TryEnable(handler));

		Skip.If(error is not null, $"Reduce Motion is on and MAUI's animations could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync() => new(InvokeOnMainThreadAsync(reduceMotionOverride.Restore));
}