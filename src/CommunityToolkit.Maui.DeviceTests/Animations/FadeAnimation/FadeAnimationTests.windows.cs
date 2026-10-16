using Microsoft.UI.Xaml;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	static double GetNativeOpacity(IPlatformViewHandler handler) => ((FrameworkElement)(handler.ContainerView ?? handler.PlatformView!)).Opacity;

	// No-op on Windows: MAUI's Windows ticker animates whatever the system's animation setting (MAUI 10 does not read it).
	static Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	static ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;
}