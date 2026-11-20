using System.Globalization;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AnimationTestBase
{
	readonly SystemAnimationsTickerOverride reduceMotionOverride = new(static () => UIAccessibility.IsReduceMotionEnabled);

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetTransformedView(handler).Alpha;

	// .NET MAUI's mappers write to the container view when the handler has one (ToPlatform())
	static UIView GetTransformedView(IPlatformViewHandler handler) => (UIView)(handler.ContainerView ?? handler.PlatformView!);

	static string DescribeNativeValues(IPlatformViewHandler handler) => DescribeNativeValues(GetTransformedView(handler));

	// UpdateTransformation reads the frame and parent of the .NET MAUI view, not of the platform view, so a view that no window shows gets the same transform
	static string DescribeExpectedNativeValues(IPlatformViewHandler handler, VisualElement view)
	{
		using var expectedView = new UIView();
		expectedView.UpdateOpacity(view);
		expectedView.UpdateTransformation(view);
		return DescribeNativeValues(expectedView);
	}

	static string DescribeNativeValues(UIView view)
	{
		var transform = view.Layer.Transform;
		double[] matrix = [transform.M11, transform.M12, transform.M13, transform.M14, transform.M21, transform.M22, transform.M23, transform.M24, transform.M31, transform.M32, transform.M33, transform.M34, transform.M41, transform.M42, transform.M43, transform.M44];
		return string.Create(CultureInfo.InvariantCulture,
			$"alpha {FormatNativeValue(view.Alpha)}, anchor ({FormatNativeValue(view.Layer.AnchorPoint.X)}, {FormatNativeValue(view.Layer.AnchorPoint.Y)}), transform [{string.Join(' ', matrix.Select(FormatNativeValue))}]");
	}

	// Center and Bounds, as .NET MAUI arranges the view (Frame is undefined under a transform), and where the transformed view is in its window
	static string DescribeNativeLayout(IPlatformViewHandler handler)
	{
		var view = GetTransformedView(handler);
		var boundsInWindow = view.ConvertRectToView(view.Bounds, null);
		return string.Create(CultureInfo.InvariantCulture,
			$"center ({FormatNativeValue(view.Center.X)}, {FormatNativeValue(view.Center.Y)}), bounds ({FormatNativeValue(view.Bounds.Width)}, {FormatNativeValue(view.Bounds.Height)}), in window ({FormatNativeValue(boundsInWindow.X)}, {FormatNativeValue(boundsInWindow.Y)}, {FormatNativeValue(boundsInWindow.Width)}, {FormatNativeValue(boundsInWindow.Height)})");
	}

	// With Reduce Motion on, MAUI 11 jumps to the end of every animation, so no animation could start while another one runs.
	protected async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		string? error = null;
		await InvokeOnMainThreadAsync(() => error = reduceMotionOverride.TryEnable(handler));

		Skip.If(error is not null, $"Reduce Motion is on and MAUI's animations could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync() => new(InvokeOnMainThreadAsync(reduceMotionOverride.Restore));
}