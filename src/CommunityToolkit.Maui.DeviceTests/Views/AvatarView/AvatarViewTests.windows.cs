using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace CommunityToolkit.Maui.DeviceTests;

// MAUI's BorderHandler adds the border path (a Microsoft.UI.Xaml.Shapes.Path) next to the content, which MAUI 11.0.10 and later put in an internal
// clip host panel (dotnet/maui#37424). MAUI wraps every AspectFill image and every label whose VerticalTextAlignment is not Start in a WrapperView.
public partial class AvatarViewTests
{
	static bool BuildsFallbackClipGeometry => true;

	// The layout slot that the parent (the WrapperView, whose Width and Height carry the size request) arranged the image in, not its rendered
	// size (ActualWidth/ActualHeight): a 16x16 bitmap with Stretch UniformToFill renders square in a slot that is not, and the layout clip cuts
	// the part outside the slot.
	static Size NativeSizeOf(IView view)
	{
		var platformView = (FrameworkElement)view.Handler!.PlatformView!;
		var slot = LayoutInformation.GetLayoutSlot(platformView);
		return new Size(slot.Width, slot.Height);
	}

	static NativeContent NativeContentOf(IPlatformViewHandler handler)
	{
		var child = ContentElementOf((Panel)handler.PlatformView!);
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

	static UIElement? ContentElementOf(Panel borderPanel)
	{
		var child = borderPanel.Children.FirstOrDefault(static element => element is not Microsoft.UI.Xaml.Shapes.Path);
		return child is Panel contentClipHost and not WrapperView ? contentClipHost.Children.FirstOrDefault() : child;
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