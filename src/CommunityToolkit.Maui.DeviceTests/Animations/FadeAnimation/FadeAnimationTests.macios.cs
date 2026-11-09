using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	static double GetNativeOpacity(IPlatformViewHandler handler) => ((UIView)(handler.ContainerView ?? handler.PlatformView!)).Alpha;

	// Nothing to enable: Core Animation always animates, and no system switch makes MAUI skip animations.
	static Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	static ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;
}