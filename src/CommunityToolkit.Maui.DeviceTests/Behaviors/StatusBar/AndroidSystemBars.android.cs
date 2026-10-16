#pragma warning disable CA1422, CS0618 // Window.StatusBarColor / NavigationBarColor / SystemUiVisibility are obsolete from API 30-35 but still readable: the tests read the native state of every API level.
using System.Reflection;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Reads and resets the native state of an Android window that the status bar and navigation bar code changes. Everything here
/// goes through the Android SDK (and AndroidX), never through a toolkit field. Call it on the UI thread.
/// </summary>
static class AndroidSystemBars
{
	// Tags that the toolkit puts on the overlay views it adds to the decor view (StatusBar.android.cs, NavigationBar.android.cs).
	public const string StatusBarOverlayTag = "StatusBarOverlay";
	public const string NavigationBarOverlayTag = "NavigationBarOverlay";

	// SYSTEM_UI_FLAG_LAYOUT_STABLE | SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION | SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN: what
	// WindowCompat.SetDecorFitsSystemWindows(window, false) sets on API < 30 (read back through the decor view).
	public const int LayoutSystemUiFlags = 0x100 | 0x200 | 0x400;

	// SYSTEM_UI_FLAG_LAYOUT_STABLE: what WindowCompat.SetDecorFitsSystemWindows(window, false) sets on API 30-34, next to
	// Window.setDecorFitsSystemWindows (AndroidX Core's WindowCompat.Api30Impl); from API 35 on it only calls Window.setDecorFitsSystemWindows.
	public const int LayoutStableSystemUiFlag = 0x100;

	// WindowInsetsController.APPEARANCE_FORCE_LIGHT_NAVIGATION_BARS, a hidden flag: Android 15+ sets it on an edge-to-edge window that has no
	// navigation bar color of its own and a light background (DecorView.setWindowBackground), and then shows light navigation bars (dark icons)
	// whatever APPEARANCE_LIGHT_NAVIGATION_BARS says. Window.setNavigationBarColor clears it.
	public const int ForceLightNavigationBarsAppearance = 1 << 9;

	// The window flags that StatusBar.android.cs and AndroidWindowExtensions.GetCurrentWindow change.
	const WindowManagerFlags systemBarFlags = WindowManagerFlags.LayoutNoLimits | WindowManagerFlags.TranslucentStatus | WindowManagerFlags.DrawsSystemBarBackgrounds;

	static WindowState? originalState;

	public static Android.App.Activity Activity => Platform.CurrentActivity ?? throw new InvalidOperationException("The test needs the runner's current activity.");

	/// <summary>The activity's own window (not the toolkit's GetCurrentWindow, which changes flags and prefers a visible dialog).</summary>
	public static AWindow ActivityWindow => Activity.Window ?? throw new InvalidOperationException("The current activity has no window.");

	/// <summary>
	/// The system bar state of the runner's window before the first status bar test changed it (captured once per app run). <see cref="Reset"/>
	/// puts it back, so the tests that run later in the same app see the window as the runner set it up (e.g. the theme's status bar color on
	/// API 28, which is not black).
	/// </summary>
	public static WindowState OriginalState => originalState ?? throw new InvalidOperationException($"{nameof(CaptureOriginalState)} has not run yet.");

	public static ViewGroup Decor(AWindow window) => (ViewGroup)window.DecorView;

	public static WindowManagerFlags Flags(AWindow window) => window.Attributes?.Flags ?? 0;

	public static bool HasFlag(AWindow window, WindowManagerFlags flag) => (Flags(window) & flag) == flag;

	public static int SystemUiVisibility(AWindow window) => (int)window.DecorView.SystemUiVisibility;

	public static bool IsLightStatusBars(AWindow window) => WindowCompat.GetInsetsController(window, window.DecorView)?.AppearanceLightStatusBars ?? false;

	public static bool IsLightNavigationBars(AWindow window) => WindowCompat.GetInsetsController(window, window.DecorView)?.AppearanceLightNavigationBars ?? false;

	/// <summary>Whether the system forces light navigation bars (dark icons) on the window (Android 15+, see <see cref="ForceLightNavigationBarsAppearance"/>).</summary>
	public static bool IsForcedLightNavigationBars(AWindow window) =>
		OperatingSystem.IsAndroidVersionAtLeast(30) && ((window.InsetsController?.SystemBarsAppearance ?? 0) & ForceLightNavigationBarsAppearance) != 0;

	/// <summary>All direct children of the decor view that carry <paramref name="tag"/>.</summary>
	public static List<AView> FindTagged(AWindow window, string tag)
	{
		var decor = Decor(window);
		var tagged = new List<AView>();
		for (var i = 0; i < decor.ChildCount; i++)
		{
			var child = decor.GetChildAt(i);
			if (child?.Tag?.ToString() == tag)
			{
				tagged.Add(child);
			}
		}

		return tagged;
	}

	public static AView? FindOverlay(AWindow window, string tag) => FindTagged(window, tag).FirstOrDefault();

	public static Android.Graphics.Color? BackgroundColor(AView view) => (view.Background as Android.Graphics.Drawables.ColorDrawable)?.Color;

	/// <summary>
	/// Asserts that <paramref name="overlay"/> is drawn above the window's content, the decor child that holds android.R.id.content (see
	/// <see cref="AssertDrawnAbove"/>).
	/// </summary>
	public static void AssertDrawnAboveTheContent(AWindow window, AView overlay)
	{
		var decor = Decor(window);
		var contentChild = window.FindViewById(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The window has no android.R.id.content view.");
		while (decor.IndexOfChild(contentChild) < 0)
		{
			contentChild = contentChild.Parent as AView ?? throw new InvalidOperationException("android.R.id.content is not inside the decor view.");
		}

		AssertDrawnAbove(window, overlay, contentChild);
	}

	/// <summary>
	/// Asserts that the decor view draws its child <paramref name="overlay"/> above its child <paramref name="other"/>: a view group draws its
	/// children in the order of their elevation (Z), and children at the same elevation in child order.
	/// </summary>
	public static void AssertDrawnAbove(AWindow window, AView overlay, AView other)
	{
		var decor = Decor(window);
		var overlayIndex = decor.IndexOfChild(overlay);
		var otherIndex = decor.IndexOfChild(other);
		Assert.True(overlayIndex >= 0 && otherIndex >= 0, $"Both views have to be children of the decor view (decor children {overlayIndex} and {otherIndex}).");

		var isDrawnAbove = overlay.GetZ().CompareTo(other.GetZ()) switch
		{
			> 0 => true,
			0 => overlayIndex > otherIndex,
			_ => false
		};
		Assert.True(isDrawnAbove, $"The overlay (decor child {overlayIndex}, Z {overlay.GetZ()}) is drawn behind decor child {otherIndex} (Z {other.GetZ()}).");
	}

	/// <summary>The bounds of <paramref name="view"/> in its parent, as its last layout placed it.</summary>
	public static Android.Graphics.Rect Frame(AView view) => new(view.Left, view.Top, view.Right, view.Bottom);

	/// <summary>
	/// The insets of the window's navigation bar as the window reports them to its root view (API 30+): what the system reports, before any view
	/// of the window consumed some. A hidden navigation bar, or one that the window does not reach, has none.
	/// </summary>
	public static Android.Graphics.Insets NavigationBarInsets(AWindow window)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			throw new PlatformNotSupportedException("WindowInsets.Type.NavigationBars() needs API 30.");
		}

		return window.DecorView.RootWindowInsets?.GetInsets(WindowInsets.Type.NavigationBars()) ?? Android.Graphics.Insets.None;
	}

	/// <summary>
	/// Where the window's navigation bar is, in the coordinates of the decor view (API 30+). The decor view places its own navigation bar background
	/// the same way: at the bottom, or, when the navigation bar has no bottom inset, at the right or left edge (three-button navigation in landscape).
	/// </summary>
	public static Android.Graphics.Rect NavigationBarFrame(AWindow window)
	{
		var decor = window.DecorView;
		var insets = NavigationBarInsets(window);

		return NavigationBarEdge(insets) switch
		{
			GravityFlags.Right => new(decor.Width - insets.Right, 0, decor.Width, decor.Height),
			GravityFlags.Left => new(0, 0, insets.Left, decor.Height),
			_ => new(0, decor.Height - insets.Bottom, decor.Width, decor.Height)
		};
	}

	/// <summary>The width, height and gravity of a child of the decor view that covers the window's navigation bar (see <see cref="NavigationBarFrame"/>).</summary>
	public static (int Width, int Height, GravityFlags Gravity) NavigationBarLayout(AWindow window)
	{
		var insets = NavigationBarInsets(window);

		return NavigationBarEdge(insets) switch
		{
			GravityFlags.Right => (insets.Right, ViewGroup.LayoutParams.MatchParent, GravityFlags.Right),
			GravityFlags.Left => (insets.Left, ViewGroup.LayoutParams.MatchParent, GravityFlags.Left),
			_ => (ViewGroup.LayoutParams.MatchParent, insets.Bottom, GravityFlags.Bottom)
		};
	}

	// The edge of the window where the navigation bar is: the bottom, unless the bar has no bottom inset but one at the right or the left (DecorView's
	// isNavBarToRightEdge and isNavBarToLeftEdge)
	static GravityFlags NavigationBarEdge(Android.Graphics.Insets insets) => insets switch
	{
		{ Bottom: 0, Right: > 0 } => GravityFlags.Right,
		{ Bottom: 0, Left: > 0 } => GravityFlags.Left,
		_ => GravityFlags.Bottom
	};

	/// <summary>The status bar height that the window reports (API 30+).</summary>
	[System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
	public static int StatusBarInset(AWindow window) => window.DecorView.RootWindowInsets?.GetInsets(WindowInsets.Type.StatusBars()).Top ?? 0;

	/// <summary>The height of the dimen that the platform names <paramref name="name"/> in the android package (e.g. status_bar_height).</summary>
	public static int FrameworkDimension(string name)
	{
		var resources = Activity.Resources ?? throw new InvalidOperationException("The current activity has no resources.");
		var id = resources.GetIdentifier(name, "dimen", "android");
		return id > 0 ? resources.GetDimensionPixelSize(id) : 0;
	}

	/// <summary>Runs everything that was posted to the window's decor view before this call (FIFO), e.g. the API 36 status bar overlay creation.</summary>
	public static async Task FlushPostedActions(AWindow window)
	{
		var completion = new TaskCompletionSource();
		window.DecorView.Post(() => completion.TrySetResult());

		// A decor view that is not attached never runs its queue: fail with a TimeoutException instead of hanging the run.
		await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}

	/// <summary>
	/// Runs <paramref name="action"/> while MAUI reports no current activity (<c>Platform.CurrentActivity</c> is <see langword="null"/>), which is
	/// what the toolkit's <c>Activity can't be null</c> guard and the behavior's <c>Platform.CurrentActivity is null</c> check react to.
	/// </summary>
	public static T WithoutCurrentActivity<T>(Func<T> action) => WithCurrentActivity(null, action);

	/// <summary>
	/// Runs <paramref name="action"/> while MAUI reports <paramref name="activity"/> as the current activity (<c>Platform.CurrentActivity</c>), which
	/// is where the toolkit's status bar code takes its window from. The runner's activity is restored afterwards. MAUI's ActivityStateManager
	/// keeps it in a lifecycle listener; there is no public way to replace it.
	/// </summary>
	public static T WithCurrentActivity<T>(Android.App.Activity? activity, Func<T> action)
	{
		var manager = ActivityStateManager.Default;
		var listener = manager.GetType().GetField("lifecycleListener", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.GetValue(manager)
			?? throw new InvalidOperationException("MAUI's ActivityStateManager no longer keeps its activity in a 'lifecycleListener' field.");
		var activityProperty = listener.GetType().GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException("MAUI's ActivityLifecycleContextListener no longer has an 'Activity' property.");

		var runnerActivity = activityProperty.GetValue(listener);
		activityProperty.SetValue(listener, activity);
		try
		{
			return action();
		}
		finally
		{
			activityProperty.SetValue(listener, runnerActivity);
		}
	}

	/// <summary>Records the runner window's system bar state the first time it is called; later calls do nothing.</summary>
	public static void CaptureOriginalState(AWindow window) => originalState ??= WindowState.Read(window);

	/// <summary>Removes the toolkit's overlays and puts the window's flags, bar colors and appearance back to <see cref="OriginalState"/>.</summary>
	public static void Reset(AWindow window)
	{
		CaptureOriginalState(window);
		Restore(window, OriginalState);
	}

	/// <summary>
	/// Removes the toolkit's overlays and puts the window's flags, bar colors and appearance back to <paramref name="original"/>, e.g. a
	/// <see cref="WindowState.Read"/> taken before a test outside the status bar tests let the toolkit change the runner's window. Such a
	/// test has to put the window back: the status bar tests capture the runner window's state once per app run (<see cref="CaptureOriginalState"/>),
	/// so a change left behind by an earlier test would become their "original" state.
	/// </summary>
	public static void Restore(AWindow window, WindowState original)
	{
		var decor = Decor(window);
		foreach (var tag in new[] { StatusBarOverlayTag, NavigationBarOverlayTag })
		{
			foreach (var overlay in FindTagged(window, tag))
			{
				decor.RemoveView(overlay);
			}
		}

		window.SetFlags(original.Flags & systemBarFlags, systemBarFlags);

		if (OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			// API 30+ has no getter for it; the runner's window fits the system windows (the default).
			WindowCompat.SetDecorFitsSystemWindows(window, true);
		}
		else
		{
			var current = SystemUiVisibility(window);
			window.DecorView.SystemUiVisibility = (StatusBarVisibility)((current & ~LayoutSystemUiFlags) | (original.SystemUiVisibility & LayoutSystemUiFlags));
		}

		var controller = WindowCompat.GetInsetsController(window, window.DecorView);
		if (controller is not null)
		{
			controller.AppearanceLightStatusBars = original.LightStatusBars;
			controller.AppearanceLightNavigationBars = original.LightNavigationBars;
		}

		window.SetStatusBarColor(new Android.Graphics.Color(original.StatusBarColor));
		window.SetNavigationBarColor(new Android.Graphics.Color(original.NavigationBarColor));
	}

	/// <summary>The parts of a window's state that the status bar and navigation bar code change.</summary>
	public sealed record WindowState(WindowManagerFlags Flags, int SystemUiVisibility, int StatusBarColor, int NavigationBarColor, bool LightStatusBars, bool LightNavigationBars)
	{
		public bool HasFlag(WindowManagerFlags flag) => (Flags & flag) == flag;

		public static WindowState Read(AWindow window) => new(
			AndroidSystemBars.Flags(window),
			AndroidSystemBars.SystemUiVisibility(window),
			window.StatusBarColor,
			window.NavigationBarColor,
			IsLightStatusBars(window),
			IsLightNavigationBars(window));
	}
}