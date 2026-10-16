using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CommunityToolkit.Maui.DeviceTests;

// Windows: the avatar is a ContentPanel. Its children are the border path that MAUI's BorderHandler always adds first (a
// Microsoft.UI.Xaml.Shapes.Path, MAUI 10.0.90 BorderHandler.Windows UpdateContent) and the content: an Image or a TextBlock, each inside a
// WrapperView (MAUI wraps every AspectFill image, and every label whose VerticalTextAlignment is not Start). Without a stroke shape the
// toolkit clips the image with a RoundRectangleGeometry whose origin is (0, 0), the image's origin, as on Android.
public partial class AvatarViewTests
{
	static bool BuildsFallbackClipGeometry => true;

	// The size the native image was laid out with: the layout slot that its parent (the WrapperView, whose Width and Height carry the size
	// request) arranged it in. The rendered size (ActualWidth/ActualHeight) is no oracle: a 16x16 bitmap with Stretch UniformToFill renders
	// square in a slot that is not, and the layout clip cuts the part outside the slot.
	static Size NativeSizeOf(IView view)
	{
		var platformView = (FrameworkElement)view.Handler!.PlatformView!;
		var slot = LayoutInformation.GetLayoutSlot(platformView);
		return new Size(slot.Width, slot.Height);
	}

	static NativeContent NativeContentOf(IPlatformViewHandler handler)
	{
		// The border path is not content: the content is the other child.
		var child = ((Panel)handler.PlatformView!).Children.FirstOrDefault(static element => element is not Microsoft.UI.Xaml.Shapes.Path);
		if (child is WrapperView wrapper)
		{
			child = wrapper.Child;
		}

		return child switch
		{
			Microsoft.UI.Xaml.Controls.Image => NativeContent.Image,
			TextBlock => NativeContent.Label,
			_ => NativeContent.None,
		};
	}

	static void AssertFallbackClip(AvatarView avatar, Microsoft.Maui.Controls.Image image, double imageWidth, double imageHeight)
	{
		var geometry = Assert.IsType<RoundRectangleGeometry>(image.Clip);

		Assert.Equal(avatar.CornerRadius, geometry.CornerRadius);
		Assert.Equal(0, geometry.Rect.X);
		Assert.Equal(0, geometry.Rect.Y);
		Assert.Equal(imageWidth, geometry.Rect.Width, 0.01);
		Assert.Equal(imageHeight, geometry.Rect.Height, 0.01);
	}

	static async Task<Color> ColorAtAsync(IPlatformViewHandler handler, double x, double y)
	{
		var view = (FrameworkElement)handler.PlatformView!;
		var scale = view.XamlRoot.RasterizationScale;
		var bitmap = await view.ToBitmap(handler.MauiContext!);

		var color = bitmap.ColorAtPoint((int)(x * scale), (int)(y * scale), includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}
}