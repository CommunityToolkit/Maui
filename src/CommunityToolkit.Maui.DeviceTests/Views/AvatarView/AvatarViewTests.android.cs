using Android.Views;
using Android.Widget;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

// Android: the avatar is a ContentViewGroup whose child is an ImageView or a TextView (inside a WrapperView while it has a clip). Without
// a stroke shape the toolkit clips the image with a RoundRectangleGeometry.
public partial class AvatarViewTests
{
	static bool BuildsFallbackClipGeometry => true;

	static Size NativeSizeOf(IView view)
	{
		var platformView = (AView)view.Handler!.PlatformView!;
		var density = platformView.Context!.Resources!.DisplayMetrics!.Density;
		return new Size(platformView.Width / density, platformView.Height / density);
	}

	static NativeContent NativeContentOf(IPlatformViewHandler handler)
	{
		var group = (ViewGroup)handler.PlatformView!;
		if (group.ChildCount == 0)
		{
			return NativeContent.None;
		}

		var child = group.GetChildAt(0);
		if (child is WrapperView wrapper)
		{
			child = wrapper.GetChildAt(0);
		}

		return child switch
		{
			ImageView => NativeContent.Image,
			TextView => NativeContent.Label,
			_ => NativeContent.None,
		};
	}

	// The geometry starts at the image's origin and has the size of the image and the avatar's corner radius.
	static void AssertFallbackClip(AvatarView avatar, Image image, double imageWidth, double imageHeight)
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
		var view = (AView)handler.PlatformView!;
		var density = view.Context!.Resources!.DisplayMetrics!.Density;
		var bitmap = await view.ToBitmap(handler.MauiContext!);

		var color = bitmap.ColorAtPoint((int)(x * density), (int)(y * density), includeAlpha: true);

		return Color.FromRgba(color.R, color.G, color.B, color.A);
	}
}