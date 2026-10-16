#pragma warning disable CA1422 // UIApplication.Windows is obsolete since iOS 15, but StatusBar.ios.cs still iterates it: the tests read the same windows.
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Reads and resets the native state that StatusBar.ios.cs changes: a UIView with the tag 38482 in every UIWindow of the app. Call it on the UI thread.
/// </summary>
static class IosStatusBarViews
{
	/// <summary>The tag that StatusBar.ios.cs puts on the view it adds to every window.</summary>
	public const int StatusBarTag = 38482;

	/// <summary>
	/// The windows that the status bar code iterates. Not only the runner's window: once a text input of the app was first responder (in any
	/// earlier test of the app run), UIKit keeps a second window of the runner's scene listed, which gets a status bar view of its own. So
	/// assert per window, never on the number of status bar views of all windows.
	/// </summary>
	public static UIWindow[] Windows => UIApplication.SharedApplication.Windows;

	/// <summary>The window's direct subviews that carry the status bar tag.</summary>
	public static List<UIView> TaggedSubviews(UIWindow window) => window.Subviews.Where(static view => view.Tag == StatusBarTag).ToList();

	/// <summary>Every status bar view of every window (their number depends on how many windows UIKit lists, see <see cref="Windows"/>).</summary>
	public static List<UIView> AllTaggedSubviews() => Windows.SelectMany(TaggedSubviews).ToList();

	/// <summary>The status bar frame that UIKit reports for the window's scene, or <see langword="null"/> when the window has no scene.</summary>
	public static CGRect? SceneStatusBarFrame(UIWindow window) => window.WindowScene?.StatusBarManager?.StatusBarFrame;

	/// <summary>The windows that the toolkit processes (the ones whose scene reports a status bar frame).</summary>
	public static List<UIWindow> WindowsWithStatusBar() => Windows.Where(static window => SceneStatusBarFrame(window) is not null).ToList();

	/// <summary>The scene of the runner's window, which every additional test window joins.</summary>
	public static UIWindowScene RunnerScene => Windows.FirstOrDefault(static window => window.WindowScene is not null)?.WindowScene ?? throw new InvalidOperationException("The runner's window has no scene.");

	/// <summary>Adds a small visible window to the runner's scene (UIApplication.Windows lists it from then on).</summary>
	public static UIWindow AddWindow(UIViewController? rootViewController = null) =>
		new(RunnerScene) { Frame = new CGRect(0, 0, 100, 100), RootViewController = rootViewController, Hidden = false };

	/// <summary>Takes a window added with <see cref="AddWindow"/> out of the scene again (a hidden window stays listed until it leaves its scene).</summary>
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

	/// <summary>Compares the red, green, blue and alpha components of a MAUI color with a UIKit color (2 decimals).</summary>
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

/// <summary>
/// A window of the runner's scene that can report no scene (<see cref="UIWindow.WindowScene"/> is virtual) while <see cref="HidesScene"/> is set.
/// UIApplication.Windows still lists it then (UIKit keeps it in its scene), so the status bar code meets a listed window without a status bar
/// frame, the case of a window whose scene has no StatusBarManager (an external display or CarPlay), which the simulator cannot create.
/// Set <see cref="HidesScene"/> only around the synchronous call under test: UIKit reads the window's scene too.
/// </summary>
sealed class SceneHidingWindow(UIWindowScene scene) : UIWindow(scene)
{
	public bool HidesScene { get; set; }

	public override UIWindowScene? WindowScene
	{
		get => HidesScene ? null : base.WindowScene;
		set => base.WindowScene = value;
	}
}