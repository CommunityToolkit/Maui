#pragma warning disable CA1422 // UIApplication.Windows is obsolete since iOS 15, but StatusBar.ios.cs still iterates it: the tests read the same windows.
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// Call it on the UI thread.
static class IosStatusBarViews
{
	// The tag that StatusBar.ios.cs puts on the view it adds to every window of the app.
	public const int StatusBarTag = 38482;

	// Not only the runner's window: once a text input of the app was first responder (in any earlier test of the app run), UIKit keeps a
	// second window of the runner's scene listed, which gets a status bar view of its own too, because the runner's app has no .NET MAUI window
	// and StatusBar then takes every listed window for one of the app's. So assert per window, never on the number of status bar views of all
	// windows.
	public static UIWindow[] Windows => UIApplication.SharedApplication.Windows;

	public static List<UIView> TaggedSubviews(UIWindow window) => window.Subviews.Where(static view => view.Tag == StatusBarTag).ToList();

	public static List<UIView> AllTaggedSubviews() => Windows.SelectMany(TaggedSubviews).ToList();

	public static CGRect? SceneStatusBarFrame(UIWindow window) => window.WindowScene?.StatusBarManager?.StatusBarFrame;

	public static List<UIWindow> WindowsWithStatusBar() => Windows.Where(static window => SceneStatusBarFrame(window) is not null).ToList();

	public static UIWindow RunnerWindow => Windows.FirstOrDefault(static window => window.WindowScene is not null) ?? throw new InvalidOperationException("The runner has no window with a scene.");

	public static UIWindowScene RunnerScene => RunnerWindow.WindowScene ?? throw new InvalidOperationException("The runner's window has no scene.");

	public static UIWindow AddWindow(UIViewController? rootViewController = null) =>
		new(RunnerScene) { Frame = new CGRect(0, 0, 100, 100), RootViewController = rootViewController, Hidden = false };

	// A hidden window stays listed until it leaves its scene.
	public static void RemoveWindow(UIWindow window)
	{
		window.Hidden = true;
		window.RootViewController = null;
		window.WindowScene = null;
		window.Dispose();
	}

	public static void Reset()
	{
		foreach (var window in Windows)
		{
			foreach (var view in TaggedSubviews(window))
			{
				view.RemoveFromSuperview();
			}
		}
	}

	public static void AssertColor(Color expected, UIColor? actual)
	{
		Assert.NotNull(actual);
		actual.GetRGBA(out var red, out var green, out var blue, out var alpha);
		Assert.Equal(expected.Red, (float)red, 2);
		Assert.Equal(expected.Green, (float)green, 2);
		Assert.Equal(expected.Blue, (float)blue, 2);
		Assert.Equal(expected.Alpha, (float)alpha, 2);
	}
}

// UIApplication.Windows still lists the window while HidesScene is set (UIKit keeps it in its scene), so the status bar code meets a
// listed window without a status bar frame, the case of a window whose scene has no StatusBarManager (an external display or CarPlay),
// which the simulator cannot create. Set HidesScene only around the synchronous call under test: UIKit reads the window's scene too.
sealed class SceneHidingWindow(UIWindowScene scene) : UIWindow(scene)
{
	public bool HidesScene { get; set; }

	public override UIWindowScene? WindowScene
	{
		get => HidesScene ? null : base.WindowScene;
		set => base.WindowScene = value;
	}
}