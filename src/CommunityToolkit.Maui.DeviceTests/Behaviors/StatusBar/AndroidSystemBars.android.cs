#pragma warning disable CA1422, CS0618 // Window.StatusBarColor / NavigationBarColor / SystemUiVisibility are obsolete from API 30-35 but still readable: the tests read the native state of every API level.
using System.Reflection;
using Android.Views;
using AndroidX.Core.View;
using Microsoft.Maui.ApplicationModel;
using AView = Android.Views.View;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

// Everything here goes through the Android SDK (and AndroidX), never through a toolkit field. Call it on the UI thread.
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

	// Not the toolkit's GetCurrentWindow, which changes flags and prefers a visible dialog.
	public static AWindow ActivityWindow => Activity.Window ?? throw new InvalidOperationException("The current activity has no window.");

	// Reset puts the window back to this state, so the tests that run later in the same app see the window as the runner set it up (e.g. the
	// theme's status bar color on API 28, which is not black).
	public static WindowState OriginalState => originalState ?? throw new InvalidOperationException($"{nameof(CaptureOriginalState)} has not run yet.");

	public static ViewGroup Decor(AWindow window) => (ViewGroup)window.DecorView;

	public static WindowManagerFlags Flags(AWindow window) => window.Attributes?.Flags ?? 0;

	public static bool HasFlag(AWindow window, WindowManagerFlags flag) => (Flags(window) & flag) == flag;

	public static int SystemUiVisibility(AWindow window) => (int)window.DecorView.SystemUiVisibility;

	public static bool IsLightStatusBars(AWindow window) => WindowCompat.GetInsetsController(window, window.DecorView)?.AppearanceLightStatusBars ?? false;

	public static bool IsLightNavigationBars(AWindow window) => WindowCompat.GetInsetsController(window, window.DecorView)?.AppearanceLightNavigationBars ?? false;

	public static bool IsForcedLightNavigationBars(AWindow window) =>
		OperatingSystem.IsAndroidVersionAtLeast(30) && ((window.InsetsController?.SystemBarsAppearance ?? 0) & ForceLightNavigationBarsAppearance) != 0;

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

	// An edge-to-edge window (API 35+) without DrawsSystemBarBackgrounds paints these black over the content. The decor view adds such a background
	// view only once it shows it, and hides it again with a short fade.
	public static bool PaintsASystemBarBlack(AWindow window) =>
		IsBlackBackground(window.DecorView.FindViewById(Android.Resource.Id.StatusBarBackground))
		|| IsBlackBackground(window.DecorView.FindViewById(Android.Resource.Id.NavigationBarBackground));

	static bool IsBlackBackground(AView? background) =>
		background is { Visibility: ViewStates.Visible } && BackgroundColor(background) == Android.Graphics.Color.Black;

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

	// A view group draws its children in the order of their elevation (Z), and children at the same elevation in child order.
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

	public static Android.Graphics.Rect Frame(AView view) => new(view.Left, view.Top, view.Right, view.Bottom);

	// The insets as the window reports them to its root view: what the system reports, before any view of the window consumed some.
	public static Android.Graphics.Insets NavigationBarInsets(AWindow window)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			throw new PlatformNotSupportedException("WindowInsets.Type.NavigationBars() needs API 30.");
		}

		return window.DecorView.RootWindowInsets?.GetInsets(WindowInsets.Type.NavigationBars()) ?? Android.Graphics.Insets.None;
	}

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

	// Decides as DecorView's isNavBarToRightEdge and isNavBarToLeftEdge do: three-button navigation in landscape puts the bar at the right or
	// left edge.
	static GravityFlags NavigationBarEdge(Android.Graphics.Insets insets) => insets switch
	{
		{ Bottom: 0, Right: > 0 } => GravityFlags.Right,
		{ Bottom: 0, Left: > 0 } => GravityFlags.Left,
		_ => GravityFlags.Bottom
	};

	[System.Runtime.Versioning.SupportedOSPlatform("android30.0")]
	public static int StatusBarInset(AWindow window) => window.DecorView.RootWindowInsets?.GetInsets(WindowInsets.Type.StatusBars()).Top ?? 0;

	public static int FrameworkDimension(string name)
	{
		var resources = Activity.Resources ?? throw new InvalidOperationException("The current activity has no resources.");
		var id = resources.GetIdentifier(name, "dimen", "android");
		return id > 0 ? resources.GetDimensionPixelSize(id) : 0;
	}

	// Everything posted to the decor view before this call runs first (FIFO), e.g. the API 36 status bar overlay creation.
	public static async Task FlushPostedActions(AWindow window)
	{
		var completion = new TaskCompletionSource();
		window.DecorView.Post(() => completion.TrySetResult());

		// A decor view that is not attached never runs its queue: fail with a TimeoutException instead of hanging the run.
		await completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
	}

	// The toolkit's "Activity can't be null" guard and the behavior's Platform.CurrentActivity is null check react to a missing activity.
	public static T WithoutCurrentActivity<T>(Func<T> action) => WithCurrentActivity(null, action);

	// Platform.CurrentActivity is where the toolkit's status bar code takes its window from. MAUI's ActivityStateManager keeps it in a
	// lifecycle listener; there is no public way to replace it.
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

	public static void CaptureOriginalState(AWindow window) => originalState ??= WindowState.Read(window);

	public static void Reset(AWindow window)
	{
		CaptureOriginalState(window);
		Restore(window, OriginalState);
	}

	// A test outside the status bar tests that lets the toolkit change the runner's window has to put it back: the status bar tests capture
	// the runner window's state once per app run, so a change left behind by an earlier test would become their "original" state.
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