using System.Globalization;
using Microsoft.Maui.Animations;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
using WPoint = Windows.Foundation.Point;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class AnimationTestBase
{
	readonly SystemAnimationsTickerOverride animationsOffOverride = new(static () => !new UISettings().AnimationsEnabled);

	protected static double GetNativeOpacity(IPlatformViewHandler handler) => GetTransformedView(handler).Opacity;

	// .NET MAUI's mappers write to the container view when the handler has one (ToPlatform())
	static FrameworkElement GetTransformedView(IPlatformViewHandler handler) => (FrameworkElement)(handler.ContainerView ?? handler.PlatformView!);

	static string DescribeNativeValues(IPlatformViewHandler handler) => DescribeNativeValues(GetTransformedView(handler));

	// The extensions that ViewHandler's mappers call, applied to an element that no window shows
	static string DescribeExpectedNativeValues(IPlatformViewHandler handler, VisualElement view)
	{
		var expected = new Microsoft.UI.Xaml.Controls.Border();
		expected.UpdateOpacity(view);
		expected.UpdateTransformation(view);
		return DescribeNativeValues(expected);
	}

	static string DescribeNativeValues(FrameworkElement element) => string.Create(CultureInfo.InvariantCulture,
		$"opacity {FormatNativeValue(element.Opacity)}, render transform {DescribeRenderTransform(element)}, projection {DescribeProjection(element.Projection)}");

	// The origin only matters while a transform is set, and .NET MAUI leaves the last origin in place when it removes the transform
	static string DescribeRenderTransform(FrameworkElement element) => element.RenderTransform switch
	{
		null => "none",
		MatrixTransform matrix when IsIdentity(matrix.Matrix) => "none",
		CompositeTransform composite => string.Create(CultureInfo.InvariantCulture, $"composite(rotation {FormatNativeValue(composite.Rotation)}, scale ({FormatNativeValue(composite.ScaleX)}, {FormatNativeValue(composite.ScaleY)}), translate ({FormatNativeValue(composite.TranslateX)}, {FormatNativeValue(composite.TranslateY)})) origin ({FormatNativeValue(element.RenderTransformOrigin.X)}, {FormatNativeValue(element.RenderTransformOrigin.Y)})"),
		ScaleTransform scale => string.Create(CultureInfo.InvariantCulture, $"scale ({FormatNativeValue(scale.ScaleX)}, {FormatNativeValue(scale.ScaleY)}) origin ({FormatNativeValue(element.RenderTransformOrigin.X)}, {FormatNativeValue(element.RenderTransformOrigin.Y)})"),
		var other => other.GetType().Name
	};

	static string DescribeProjection(Projection? projection) => projection switch
	{
		null => "none",
		PlaneProjection plane => string.Create(CultureInfo.InvariantCulture, $"plane(center ({FormatNativeValue(plane.CenterOfRotationX)}, {FormatNativeValue(plane.CenterOfRotationY)}), offset ({FormatNativeValue(plane.GlobalOffsetX)}, {FormatNativeValue(plane.GlobalOffsetY)}), rotation ({FormatNativeValue(plane.RotationX)}, {FormatNativeValue(plane.RotationY)}, {FormatNativeValue(plane.RotationZ)}))"),
		var other => other.GetType().Name
	};

	// A fresh element can default to an identity MatrixTransform instead of null
	static bool IsIdentity(Microsoft.UI.Xaml.Media.Matrix matrix) => matrix.M11 == 1 && matrix.M12 == 0 && matrix.M21 == 0 && matrix.M22 == 1 && matrix.OffsetX == 0 && matrix.OffsetY == 0;

	static string DescribeNativeLayout(IPlatformViewHandler handler)
	{
		var element = GetTransformedView(handler);
		var inWindow = element.TransformToVisual(null).TransformPoint(new WPoint(0, 0));
		return string.Create(CultureInfo.InvariantCulture,
			$"offset ({FormatNativeValue(element.ActualOffset.X)}, {FormatNativeValue(element.ActualOffset.Y)}), size ({FormatNativeValue(element.ActualSize.X)}, {FormatNativeValue(element.ActualSize.Y)}), in window ({FormatNativeValue(inWindow.X)}, {FormatNativeValue(inWindow.Y)})");
	}

	// With "Show animations in Windows" off, MAUI 11 jumps to the end of every animation, so no animation could start while another one runs.
	protected async Task EnableAnimationsAsync(IPlatformViewHandler handler)
	{
		string? error = null;
		await InvokeOnMainThreadAsync(() => error = animationsOffOverride.TryEnable(handler));

		Skip.If(error is not null, $"Windows animations are off and MAUI's animations could not be switched on for this process ({error}), so MAUI jumps to the end of every animation.");

		var ticker = handler.MauiContext?.Services.GetService(typeof(ITicker)) as ITicker;
		Assert.True(ticker is null || ticker.SystemEnabled, "MAUI's ticker must see the enabled animations.");
	}

	ValueTask PlatformDisposeAsync() => new(InvokeOnMainThreadAsync(animationsOffOverride.Restore));
}