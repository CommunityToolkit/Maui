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
		using var expected = new UIView();
		expected.UpdateOpacity(view);
		expected.UpdateTransformation(view);
		return DescribeNativeValues(expected);
	}

	static string DescribeNativeValues(UIView view)
	{
		var t = view.Layer.Transform;
		double[] matrix = [t.M11, t.M12, t.M13, t.M14, t.M21, t.M22, t.M23, t.M24, t.M31, t.M32, t.M33, t.M34, t.M41, t.M42, t.M43, t.M44];
		return string.Create(CultureInfo.InvariantCulture,
			$"alpha {FormatNativeValue(view.Alpha)}, anchor ({FormatNativeValue(view.Layer.AnchorPoint.X)}, {FormatNativeValue(view.Layer.AnchorPoint.Y)}), transform [{string.Join(' ', matrix.Select(FormatNativeValue))}]");
	}

	// Center and Bounds, as .NET MAUI arranges the view (Frame is undefined under a transform), and where the transformed view is in its window
	static string DescribeNativeLayout(IPlatformViewHandler handler)
	{
		var view = GetTransformedView(handler);
		var inWindow = view.ConvertRectToView(view.Bounds, null);
		return string.Create(CultureInfo.InvariantCulture,
			$"center ({FormatNativeValue(view.Center.X)}, {FormatNativeValue(view.Center.Y)}), bounds ({FormatNativeValue(view.Bounds.Width)}, {FormatNativeValue(view.Bounds.Height)}), in window ({FormatNativeValue(inWindow.X)}, {FormatNativeValue(inWindow.Y)}, {FormatNativeValue(inWindow.Width)}, {FormatNativeValue(inWindow.Height)})");
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