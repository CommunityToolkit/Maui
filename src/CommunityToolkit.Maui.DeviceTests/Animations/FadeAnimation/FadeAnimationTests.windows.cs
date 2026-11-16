using Microsoft.Maui.Animations;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	readonly SystemAnimationsTickerOverride animationsOffOverride = new(static () => !new UISettings().AnimationsEnabled);

	static double GetNativeOpacity(IPlatformViewHandler handler) => ((FrameworkElement)(handler.ContainerView ?? handler.PlatformView!)).Opacity;

	// With "Show animations in Windows" off, MAUI 11 jumps to the end of every animation, so no animation could start while another one runs.
	async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		string? error = null;
		await InvokeOnMainThreadAsync(() => error = animationsOffOverride.TryEnable(handler));

		Skip.If(error is not null, $"Windows animations are off and MAUI's animations could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync() => new(InvokeOnMainThreadAsync(animationsOffOverride.Restore));
}