using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// iOS and Mac Catalyst: the avatar is a ContentView whose subview is a UIImageView or a UILabel (inside a WrapperView while it has a
// clip). The toolkit builds no clip geometry here (UIKit clips the content to the border shape), so a missing stroke shape leaves the
// image without a clip.
public partial class AvatarViewTests
{
	static bool BuildsFallbackClipGeometry => false;

	static Size NativeSizeOf(IView view)
	{
		var frame = ((UIView)view.Handler!.PlatformView!).Frame;
		return new Size(frame.Width, frame.Height);
	}

	static NativeContent NativeContentOf(IPlatformViewHandler handler)
	{
		var child = ((UIView)handler.PlatformView!).Subviews.FirstOrDefault();
		if (child is WrapperView wrapper)
		{
			child = wrapper.Subviews.FirstOrDefault();
		}

		return child switch
		{
			UIImageView => NativeContent.Image,
			UILabel => NativeContent.Label,
			_ => NativeContent.None,
		};
	}

	static void AssertFallbackClip(AvatarView avatar, Image image, double imageWidth, double imageHeight) =>
		Assert.Null(image.Clip);

	static async Task<Color> ColorAtAsync(IPlatformViewHandler handler, double x, double y)
	{
		var view = (UIView)handler.PlatformView!;
		var bitmap = await view.ToBitmap(handler.MauiContext!);
		var scale = (double)bitmap.CurrentScale;

		var color = bitmap.ColorAtPoint((int)(x * scale), (int)(y * scale));
		color.GetRGBA(out var red, out var green, out var blue, out var alpha);

		return new Color((float)red, (float)green, (float)blue, (float)alpha);
	}
}