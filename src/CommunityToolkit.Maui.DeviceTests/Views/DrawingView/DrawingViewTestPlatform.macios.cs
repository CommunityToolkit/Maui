using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

static partial class DrawingViewTestPlatform
{
	internal static MauiDrawingView CreateView(double width, double height) => new() { Frame = new CGRect(0, 0, width, height) };

	internal static Task<T> HostAsync<T>(MauiDrawingView view, IMauiContext mauiContext, Func<Task<T>> body) =>
		TestDispatcher.Current.DispatchAsync(async () => await view.AttachAndRun(body));

	internal static IDrawable? GetDrawable(MauiDrawingView view) => view.Drawable;

	internal static object? GetParent(MauiDrawingView view) => view.Superview;

	internal static bool IsChildOf(MauiDrawingView view, object platformParent) => ReferenceEquals(view.Superview, platformParent);

	internal static void TouchDown(MauiDrawingView view, double x, double y) => view.TouchesBegan(CreateTouches(x, y), null);

	internal static void TouchMove(MauiDrawingView view, double x, double y) => view.TouchesMoved(CreateTouches(x, y), null);

	internal static void TouchUp(MauiDrawingView view, double x, double y) => view.TouchesEnded(CreateTouches(x, y), null);

	internal static void TouchCancel(MauiDrawingView view, double x, double y) => view.TouchesCancelled(CreateTouches(x, y), null);

	internal static NSSet CreateTouches(double x, double y) => CreateTouches(new CGPoint(x, y), new CGPoint(x, y));

	// TouchesBegan reads the previous location and TouchesMoved the current one.
	internal static NSSet CreateTouches(CGPoint location, CGPoint previousLocation) => new(new FakeTouch(location, previousLocation));

	// UI thread only.
	internal static async Task<DrawingPixelImage> RenderAsync(MauiDrawingView view, IMauiContext mauiContext)
	{
		var image = await view.ToBitmap(mauiContext);
		return ToPixelImage(image, 1);
	}

	internal static Task<DrawingPixelImage> DecodePngAsync(Stream png)
	{
		using var data = NSData.FromStream(png) ?? throw new InvalidOperationException("The PNG stream could not be read.");
		using var image = UIImage.LoadFromData(data) ?? throw new InvalidOperationException("The PNG stream could not be decoded.");
		return Task.FromResult(ToPixelImage(image, 1));
	}

	internal static DrawingPixelImage ToPixelImage(UIImage image, double scale)
	{
		var cgImage = image.CGImage ?? throw new InvalidOperationException("The image has no CGImage.");
		var width = (int)cgImage.Width;
		var height = (int)cgImage.Height;
		var bytes = new byte[width * height * 4];

		using (var colorSpace = CGColorSpace.CreateDeviceRGB())
		using (var context = new CGBitmapContext(bytes, width, height, 8, 4 * width, colorSpace, CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast))
		{
			context.DrawImage(new CGRect(0, 0, width, height), cgImage);
		}

		var pixels = new DrawingRgba[width * height];
		for (var i = 0; i < pixels.Length; i++)
		{
			int r = bytes[i * 4], g = bytes[(i * 4) + 1], b = bytes[(i * 4) + 2], a = bytes[(i * 4) + 3];
			if (a is > 0 and < 255)
			{
				r = Math.Min(255, r * 255 / a);
				g = Math.Min(255, g * 255 / a);
				b = Math.Min(255, b * 255 / a);
			}

			pixels[i] = new DrawingRgba(r, g, b, a);
		}

		return new DrawingPixelImage(width, height, pixels, scale);
	}

	sealed class FakeTouch(CGPoint location, CGPoint previousLocation) : UITouch
	{
		public override CGPoint LocationInView(UIView? view) => location;

		public override CGPoint PreviousLocationInView(UIView? view) => previousLocation;
	}
}