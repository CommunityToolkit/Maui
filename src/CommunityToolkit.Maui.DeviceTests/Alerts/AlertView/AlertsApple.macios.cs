using System.Reflection;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Reads the UIKit alert view stack that Alert, PlatformToast and PlatformSnackbar put into the key window:
// AlertView -> UIStackView (Container) -> [RoundedView (background), PaddedLabel (message), PaddedButton (Snackbar action)].
static class AlertsApple
{
	public static UIWindow KeyWindow => TryGetKeyWindow() ?? throw new InvalidOperationException("The test app has no key window.");

	public static UIWindow? TryGetKeyWindow() => UIApplicationExtensions.GetKeyWindow(UIApplication.SharedApplication);

	public static List<AlertView> GetAlertViews() => [.. TryGetKeyWindow()?.Subviews.OfType<AlertView>() ?? []];

	public static AlertView GetSingleAlertView() => Assert.Single(GetAlertViews());

	public static UIStackView GetContainer(this AlertView alertView) => Assert.Single(alertView.Subviews.OfType<UIStackView>());

	public static RoundedView GetBackground(this AlertView alertView) => Assert.Single(alertView.GetContainer().Subviews.OfType<RoundedView>());

	public static PaddedLabel GetLabel(this AlertView alertView) => Assert.Single(alertView.Children.OfType<PaddedLabel>());

	public static PaddedButton? GetActionButton(this AlertView alertView) => alertView.Children.OfType<PaddedButton>().SingleOrDefault();

	// PaddedButton pads its title with UIButton.ContentEdgeInsets, which UIKit marks obsolete from iOS 15 on (a button without a
	// UIButtonConfiguration still honors it): the tests read what the toolkit wrote.
#pragma warning disable CA1422 // Validate platform compatibility
	public static UIEdgeInsets GetContentEdgeInsets(this UIButton button) => button.ContentEdgeInsets;
#pragma warning restore CA1422

	// Layout results are fractional (pixel snapping, fitted sizes): compare positions within half a point.
	public static void AssertClose(double expected, double actual, double tolerance = 0.6) =>
		Assert.True(Math.Abs(expected - actual) <= tolerance, $"Expected {expected} (+/- {tolerance}) but was {actual}.");

	// Asserts the RGBA components of a UIKit color against literal values (not against MAUI's own color conversion).
	public static void AssertColor(double red, double green, double blue, double alpha, UIColor? actual)
	{
		Assert.NotNull(actual);
		actual.GetRGBA(out var actualRed, out var actualGreen, out var actualBlue, out var actualAlpha);
		Assert.Equal(red, (double)actualRed, 2);
		Assert.Equal(green, (double)actualGreen, 2);
		Assert.Equal(blue, (double)actualBlue, 2);
		Assert.Equal(alpha, (double)actualAlpha, 2);
	}

	// Waits for the first layout pass of the alert view (its constraints resolve a size).
	public static async Task WaitForLayoutAsync(AlertView alertView)
	{
		var window = KeyWindow;
		await AssertHelpers.AssertEventually(() =>
		{
			window.LayoutIfNeeded();
			return alertView.Frame.Width > 0 && alertView.Frame.Height > 0;
		}, timeout: 10000, message: "The alert view was never laid out.");
	}

	// Reads every pixel of the image as RGBA bytes (premultiplied, row by row from the top).
	public static (byte[] Pixels, int Width, int Height) ReadPixels(UIImage image)
	{
		var cgImage = image.CGImage!;
		var width = (int)cgImage.Width;
		var height = (int)cgImage.Height;
		var pixels = new byte[width * height * 4];

		using var colorSpace = CGColorSpace.CreateDeviceRGB();
		using var context = new CGBitmapContext(pixels, width, height, 8, 4 * width, colorSpace, CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
		context.DrawImage(new CGRect(0, 0, width, height), cgImage);

		return (pixels, width, height);
	}

	// True when the point lies inside the rounded path that RoundedView uses as its layer mask.
	public static bool IsInsideMask(UIView view, CGPoint point)
	{
		var mask = Assert.IsType<CoreAnimation.CAShapeLayer>(view.Layer.Mask);
		return mask.Path!.ContainsPoint(point, false);
	}

	// PlatformToast.isDisposed: the only trace of PlatformToast.Dispose(bool) besides the message label it disposes.
	public static bool IsDisposed(PlatformToast platformToast) =>
		(bool)(typeof(PlatformToast).GetField("isDisposed", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(platformToast)
			?? throw new InvalidOperationException("PlatformToast.isDisposed was not found."));

	// Disposing the managed peer of a view created from managed code (a toolkit subclass such as PaddedLabel) releases the managed
	// reference that kept the native view alive, but the handle stays valid while UIKit retains the view (its stack view does): the only
	// trace is the disposed flag of the peer, a private property of NSObject (its getter where only that is kept).
	public static bool IsManagedPeerDisposed(NSObject peer)
	{
		var flags = BindingFlags.Instance | BindingFlags.NonPublic;
		var getter = typeof(NSObject).GetProperty("disposed", flags)?.GetMethod ?? typeof(NSObject).GetMethod("get_disposed", flags)
			?? throw new InvalidOperationException("NSObject.disposed was not found: this .NET for iOS version keeps the disposed state elsewhere.");
		return (bool)getter.Invoke(peer, null)!;
	}

	// Alert.timer: the NSTimer that Alert.Show schedules to dismiss the alert, null before Show and after Dismiss.
	public static NSTimer? GetTimer(Alert alert) =>
		(NSTimer?)(typeof(Alert).GetField("timer", BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Alert.timer was not found.")).GetValue(alert);

	// Asserts that the timer of the alert fires once the duration has passed since Show, which ran between the two NSDate readings
	// (seconds since the reference date) before and after: NSTimer sets its fire date to the time it is created plus the interval, and
	// uses 0.1 ms for an interval that is not positive. The margin only absorbs floating point rounding.
	public static void AssertTimerFiresAfterTheDuration(Alert alert, TimeSpan duration, double before, double after)
	{
		var timer = GetTimer(alert);
		Assert.NotNull(timer);
		Assert.True(timer.IsValid, "The timer of the alert is not valid.");
		var fireDate = timer.FireDate;
		Assert.NotNull(fireDate);
		var interval = Math.Max(duration.TotalSeconds, 0.0001);
		Assert.InRange(fireDate.SecondsSinceReferenceDate, before + interval - 0.001, after + interval + 0.001);
	}
}

public enum AlertCorner
{
	TopLeft,
	TopRight,
	BottomLeft,
	BottomRight
}

// The protected members of the toolkit's alert classes, exposed for the tests.
sealed class TestAlert : Alert
{
	public AlertView View => AlertView;
}

sealed class TestAlertView(bool shouldFillAndExpandHorizontally) : AlertView(shouldFillAndExpandHorizontally)
{
	public UIStackView ContainerView => Container;
}

sealed class TestPlatformToast(string message, UIColor backgroundColor, CGRect cornerRadius, UIColor textColor, UIFont font, double characterSpacing, nfloat padding)
	: PlatformToast(message, backgroundColor, cornerRadius, textColor, font, characterSpacing, padding)
{
	public AlertView View => AlertView;

	public void CallDispose(bool isDisposing) => Dispose(isDisposing);
}

sealed class TestPlatformSnackbar(string message, UIColor backgroundColor, CGRect cornerRadius, UIColor textColor, UIFont textFont, double characterSpacing, string actionButtonText, UIColor actionTextColor, UIFont actionButtonFont, nfloat padding)
	: PlatformSnackbar(message, backgroundColor, cornerRadius, textColor, textFont, characterSpacing, actionButtonText, actionTextColor, actionButtonFont, padding)
{
	public AlertView View => AlertView;

	public void CallDispose(bool isDisposing) => Dispose(isDisposing);
}