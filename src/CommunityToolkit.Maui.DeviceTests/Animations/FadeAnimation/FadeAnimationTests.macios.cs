using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class FadeAnimationTests
{
	static double GetNativeOpacity(IPlatformViewHandler handler) => ((UIView)(handler.ContainerView ?? handler.PlatformView!)).Alpha;

	// No-op on Apple platforms: Core Animation always animates (there is no system switch that makes MAUI skip animations).
	static Task EnableAnimationsAsync(IPlatformViewHandler handler) => Task.CompletedTask;

	static ValueTask PlatformDisposeAsync() => ValueTask.CompletedTask;
}