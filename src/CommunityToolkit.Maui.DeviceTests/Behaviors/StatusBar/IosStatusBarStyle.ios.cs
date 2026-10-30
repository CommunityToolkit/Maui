using System.Runtime.InteropServices;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Each kind puts other view controllers of .NET MAUI between the window and the page, and UIKit asks the topmost of them for the style of
// the status bar.
public enum StatusBarWindowPage
{
	ContentPage,
	NavigationPage,
	Shell,
	TabbedPage,
	FlyoutPage,
	ModalPage
}

// Call it on the UI thread.
static class IosStatusBarStyle
{
	// UIKit asks the topmost view controller presented over the root view controller, and from there the end of its
	// ChildViewControllerForStatusBarStyle chain.
	public static UIViewController StyleProvider(UIWindow window)
	{
		var controller = window.RootViewController ?? throw new InvalidOperationException("The window has no root view controller.");
		while (controller.PresentedViewController is UIViewController presented)
		{
			controller = presented;
		}

		var visited = new HashSet<IntPtr>();
		while (controller.ChildViewControllerForStatusBarStyle() is UIViewController child)
		{
			if (!visited.Add(controller.Handle))
			{
				throw new InvalidOperationException("The ChildViewControllerForStatusBarStyle chain of the window has a cycle.");
			}

			controller = child;
		}

		return controller;
	}

	// UIStatusBarStyle.LightContent has the value of the deprecated BlackTranslucent, whose name Enum.ToString() prints.
	public static string Name(UIStatusBarStyle style) => style switch
	{
		UIStatusBarStyle.Default => nameof(UIStatusBarStyle.Default),
		UIStatusBarStyle.LightContent => nameof(UIStatusBarStyle.LightContent),
		UIStatusBarStyle.DarkContent => nameof(UIStatusBarStyle.DarkContent),
		_ => style.ToString()
	};

	// UIKit draws the status bar outside the windows of the app, so UIScreen.SnapshotView and UIView.DrawViewHierarchy (public API) leave
	// it out: _UICreateScreenUIImage (private, UIKitCore) draws the screen as it is shown. On the iOS 27 simulator its status bar pixels
	// were the same as those of 'xcrun simctl io screenshot'.
	// Only the top of the screen is read: iPhone Duo shows the status bar on the right edge of its screens, so ios.ps1 creates an iPhone
	// with one built-in screen.
	public static StatusBarContent CaptureContent(UIWindowScene scene)
	{
		var statusBarFrame = scene.StatusBarManager?.StatusBarFrame ?? throw new InvalidOperationException("The scene has no status bar.");
		var createScreenImage = Dlfcn.dlsym(Dlfcn.RTLD.Default, "_UICreateScreenUIImage");
		if (createScreenImage == IntPtr.Zero)
		{
			throw new InvalidOperationException("UIKit no longer has _UICreateScreenUIImage, which the test uses to read the status bar on the screen.");
		}

		using var image = Runtime.GetNSObject<UIImage>(Marshal.GetDelegateForFunctionPointer<CreateScreenImage>(createScreenImage)(), true)
			?? throw new InvalidOperationException("_UICreateScreenUIImage returned no image.");
		var screenImage = image.CGImage ?? throw new InvalidOperationException("The image of the screen has no bitmap.");

		int width = (int)screenImage.Width, height = (int)screenImage.Height;
		var stripHeight = Math.Min(height, (int)Math.Ceiling((statusBarFrame.Y + statusBarFrame.Height) * image.CurrentScale));
		var pixels = new byte[width * stripHeight * 4];
		using (var colorSpace = CGColorSpace.CreateDeviceRGB())
		using (var context = new CGBitmapContext(pixels, width, stripHeight, 8, width * 4, colorSpace, CGImageAlphaInfo.PremultipliedLast))
		{
			// The context is as high as the status bar: its top rows receive the top rows of the screen.
			context.DrawImage(new CGRect(0, stripHeight - height, width, height), screenImage);
		}

		int lightPixels = 0, darkPixels = 0;
		for (var i = 0; i < pixels.Length; i += 4)
		{
			int red = pixels[i], green = pixels[i + 1], blue = pixels[i + 2];
			if (red > 220 && green > 220 && blue > 220)
			{
				lightPixels++;
			}
			else if (red < 40 && green < 40 && blue < 40)
			{
				darkPixels++;
			}
		}

		return new StatusBarContent(lightPixels, darkPixels, $"the top {stripHeight} rows of the {width}x{height} screen (status bar frame {statusBarFrame}, scale {image.CurrentScale})");
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	delegate IntPtr CreateScreenImage();
}

// The background is neither near-white nor near-black (the tests use red pages and bars). The time, signal and battery cover about 5,500
// pixels of the iOS 27 simulator's status bar (3x) in one color.
readonly record struct StatusBarContent(int LightPixels, int DarkPixels, string Region)
{
	const int minimumContentPixels = 500;

	public bool IsLight => LightPixels >= minimumContentPixels && DarkPixels * 10 < LightPixels;

	public bool IsDark => DarkPixels >= minimumContentPixels && LightPixels * 10 < DarkPixels;

	public override string ToString() => $"{LightPixels} near-white and {DarkPixels} near-black pixels in {Region}";
}