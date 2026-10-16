#pragma warning disable CA1422 // Window.StatusBarColor is obsolete from API 35: the tests still read it on the older APIs that use it.
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AWindow = Android.Views.Window;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	protected Task AttachPageAndRun(Page page, Func<IPlatformViewHandler, Task> action) => AttachViewAndRun(page, action);

	// Not AttachAndRun: it waits for the view's root to report IsLaidOut, which a MAUI page's root view never does on Android, and its
	// WaitForLayout leaves a layout listener and timer behind that keep the platform view alive for a second after the test, which a leak
	// test would see.
	protected Task AttachViewAndRun(VisualElement view, Func<IPlatformViewHandler, Task> action) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var handler = (IPlatformViewHandler)CreateHandler(view);
			var platformView = handler.PlatformView ?? throw new InvalidOperationException("The view has no platform view.");
			var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("The test needs the runner's current activity.");
			var content = activity.FindViewById<Android.Widget.FrameLayout>(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The activity has no content view.");

			var host = new Android.Widget.FrameLayout(activity)
			{
				LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
			};
			var size = view is Page ? Android.Views.ViewGroup.LayoutParams.MatchParent : Android.Views.ViewGroup.LayoutParams.WrapContent;
			platformView.LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(size, size);

			host.AddView(platformView);
			content.AddView(host);

			try
			{
				// The device tests share a busy machine: give the view generously long to raise Loaded.
				await view.OnLoadedAsync(TimeSpan.FromSeconds(10));
				await action(handler);
			}
			finally
			{
				content.RemoveView(host);
				host.RemoveView(platformView);
			}
		});

	protected partial Task ResetNativeStateAsync() =>
		InvokeOnMainThreadAsync(async () =>
		{
			if (Platform.CurrentActivity?.Window is not AWindow window)
			{
				return;
			}

			AndroidSystemBars.CaptureOriginalState(window);
			await AndroidSystemBars.FlushPostedActions(window);
			AndroidSystemBars.Reset(window);
		});

	// StatusBar.android.cs adds this many pixels to the status bar height when it sizes the overlay.
	protected const int overlayHeightPadding = 3;

	protected static int ExpectedOverlayHeight(AWindow window) =>
		(OperatingSystem.IsAndroidVersionAtLeast(36) ? AndroidSystemBars.StatusBarInset(window) : AndroidSystemBars.FrameworkDimension("status_bar_height")) + overlayHeightPadding;

	// The window flags do not depend on the color, transparent or not (#2351: a transparent color used to lay the window out without limits,
	// behind the status and navigation bars).
	protected async Task AssertStatusBarShowsAsync(Color color, TestDialogFragment? dialog = null)
	{
		var expected = color.ToPlatform();

		if (OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			// API 36 posts the overlay creation, so wait for it.
			await AssertEventuallyOnMainThread(
				() => AndroidSystemBars.FindOverlay(dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag) is Android.Views.View overlay && AndroidSystemBars.BackgroundColor(overlay) == expected,
				$"The status bar overlay did not show {color}.");
		}

		await InvokeOnMainThreadAsync(() =>
		{
			var window = dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow;

			if (OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
				var parameters = Assert.IsType<Android.Widget.FrameLayout.LayoutParams>(overlay.LayoutParameters);
				Assert.Equal(Android.Views.ViewGroup.LayoutParams.MatchParent, parameters.Width);
				Assert.Equal(ExpectedOverlayHeight(window), parameters.Height);
				Assert.Equal(Android.Views.GravityFlags.Top, parameters.Gravity);
				Assert.Equal(0f, overlay.GetZ());
				AndroidSystemBars.AssertDrawnAboveTheContent(window, overlay);
			}
			else
			{
				Assert.Equal(expected.ToArgb(), window.StatusBarColor);
				Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
			}

			Assert.True(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));

			if (OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				var hadLayoutNoLimits = dialog?.FlagsWhenShown.HasFlag(Android.Views.WindowManagerFlags.LayoutNoLimits) ?? AndroidSystemBars.OriginalState.HasFlag(Android.Views.WindowManagerFlags.LayoutNoLimits);
				Assert.Equal(hadLayoutNoLimits, AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));

				// From API 35 on, the decor's fitting has no getter and sets no system UI flag.
				if (!OperatingSystem.IsAndroidVersionAtLeast(35))
				{
					Assert.Equal(AndroidSystemBars.LayoutStableSystemUiFlag, AndroidSystemBars.SystemUiVisibility(window) & AndroidSystemBars.LayoutStableSystemUiFlag);
				}
			}
			else
			{
				Assert.False(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));
				Assert.Equal(AndroidSystemBars.LayoutSystemUiFlags, AndroidSystemBars.SystemUiVisibility(window) & AndroidSystemBars.LayoutSystemUiFlags);
			}
		});
	}

	protected Task AssertStatusBarUntouchedAsync() =>
		InvokeOnMainThreadAsync(() =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var original = AndroidSystemBars.OriginalState;
			Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
			Assert.Equal(original.HasFlag(Android.Views.WindowManagerFlags.LayoutNoLimits), AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));
			Assert.Equal(original.HasFlag(Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds), AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));
			Assert.Equal(original.LightStatusBars, AndroidSystemBars.IsLightStatusBars(window));
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				Assert.Equal(original.StatusBarColor, window.StatusBarColor);
			}

			if (!OperatingSystem.IsAndroidVersionAtLeast(30))
			{
				Assert.Equal(original.SystemUiVisibility & AndroidSystemBars.LayoutSystemUiFlags, AndroidSystemBars.SystemUiVisibility(window) & AndroidSystemBars.LayoutSystemUiFlags);
			}
		});

	// The same as AssertStatusBarUntouchedAsync here; on iOS a status bar view without a color can exist without the color having been applied.
	protected Task AssertStatusBarColorNotAppliedAsync() => AssertStatusBarUntouchedAsync();

	// Android's light status bars have dark content, hence DarkContent.
	protected Task AssertStatusBarStyleAsync(StatusBarStyle style) =>
		InvokeOnMainThreadAsync(() => Assert.Equal(style is StatusBarStyle.DarkContent, AndroidSystemBars.IsLightStatusBars(AndroidSystemBars.ActivityWindow)));

	// Unlike the status bar overlay, the navigation bar overlay is laid out synchronously, from the insets that the window has, so nothing is
	// waited for.
	protected Task AssertNavigationBarShowsAsync(Color color, TestDialogFragment? dialog = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var expected = color.ToPlatform();
			var window = dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow;

			if (OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
				Assert.Equal(expected, AndroidSystemBars.BackgroundColor(overlay));
				var parameters = Assert.IsType<Android.Widget.FrameLayout.LayoutParams>(overlay.LayoutParameters);
				Assert.Equal(AndroidSystemBars.NavigationBarLayout(window), (parameters.Width, parameters.Height, parameters.Gravity));
				AndroidSystemBars.AssertDrawnAboveTheContent(window, overlay);

				// The decor view's own navigation bar background (a scrim behind three-button navigation), once the decor view has one.
				if (window.DecorView.FindViewById(Android.Resource.Id.NavigationBarBackground) is Android.Views.View navigationBarBackground
					&& AndroidSystemBars.Decor(window).IndexOfChild(navigationBarBackground) >= 0)
				{
					AndroidSystemBars.AssertDrawnAbove(window, overlay, navigationBarBackground);
				}
			}
			else
			{
				Assert.Equal(expected.ToArgb(), window.NavigationBarColor);
				Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
			}
		});

	protected Task AssertNavigationBarUntouchedAsync(TestDialogFragment? dialog = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var window = dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow;
			var original = AndroidSystemBars.OriginalState;
			Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
			// A dialog's window has no light appearance.
			Assert.Equal(dialog is null && original.LightNavigationBars, AndroidSystemBars.IsLightNavigationBars(window));

			// From API 35 on the window ignores window colors and reports transparent.
			if (dialog is null && !OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				Assert.Equal(original.NavigationBarColor, window.NavigationBarColor);
			}
		});

	// Android's light navigation bars have dark content, hence DarkContent. Android 15+ can force light navigation bars whatever the window's
	// own appearance says (see AndroidSystemBars.ForceLightNavigationBarsAppearance).
	protected Task AssertNavigationBarStyleAsync(NavigationBarStyle style, TestDialogFragment? dialog = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var window = dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow;
			Assert.Equal(style is NavigationBarStyle.DarkContent, AndroidSystemBars.IsLightNavigationBars(window));
			Assert.False(AndroidSystemBars.IsForcedLightNavigationBars(window), "The system forces light navigation bars on the window, whatever its own appearance.");
		});
}