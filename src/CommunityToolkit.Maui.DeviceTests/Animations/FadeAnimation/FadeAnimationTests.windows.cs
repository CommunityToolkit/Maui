using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	static double GetNativeOpacity(IPlatformViewHandler handler) => ((FrameworkElement)(handler.ContainerView ?? handler.PlatformView!)).Opacity;

	// Nothing to enable: MAUI 10's Windows ticker does not read the system's animation setting, so it always animates.
	static Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	static ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;
}