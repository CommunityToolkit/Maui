#pragma warning disable CA1422 // Window.StatusBarColor is obsolete from API 35: the tests still read it on the older APIs that use it.
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class StatusBarTests
{
	static string UnsupportedStyleMessage => "StatusBarStyle 42 is not yet supported on Android";

	[Theory]
	[InlineData(0xFFFF0000u)]
	[InlineData(0xFF0000FFu)]
	[InlineData(0x80336699u)]
	public async Task SetColorShowsTheColorInTheStatusBar(uint argb)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var color = Color.FromUint(argb);

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(color));

		await AssertStatusBarShowsAsync(color);
	}

	// #2351: a transparent color used to clear the system bar background and lay the window out without limits, behind the status and
	// navigation bars. The window flags no longer depend on the color.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SetColorWithTransparentColorKeepsTheSystemBarBackground(bool passNull)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(Colors.Red));
		await AssertStatusBarShowsAsync(Colors.Red);

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(passNull ? null : Colors.Transparent));

		await AssertStatusBarShowsAsync(Colors.Transparent);
	}

	// What an attached StatusBarBehavior with its default color does: Transparent is the first color the window gets (#2351: the page went
	// behind the status and navigation bars on launch). On Android 16 the overlay is created by a posted action, which also applies the window flags.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SetColorWithTransparentColorAsTheFirstColorKeepsTheSystemBarBackground(bool passNull)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(passNull ? null : Colors.Transparent));

		await AssertStatusBarShowsAsync(Colors.Transparent);
	}

	[Theory]
	[InlineData(0x00FF0000u)]
	[InlineData(0x00FFFFFFu)]
	public async Task SetColorWithAFullyTransparentColorOfAnotherHueKeepsTheSystemBarBackground(uint argb)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var color = Color.FromUint(argb);
		Assert.Equal(0f, color.Alpha);

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(color));

		// Written out instead of left to the helper: a color that cannot be seen gets the window flags of every other color.
		await AssertStatusBarShowsAsync(color);
		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var hadLayoutNoLimits = OperatingSystem.IsAndroidVersionAtLeast(30) && AndroidSystemBars.OriginalState.HasFlag(Android.Views.WindowManagerFlags.LayoutNoLimits);
			Assert.Equal(hadLayoutNoLimits, AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));
			Assert.True(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));
		});
	}

	// For white with alpha 0 the Sample's StatusBarBehaviorPage showed black status and navigation bars on Android 16 while the toolkit still cleared
	// DrawsSystemBarBackgrounds for every color without alpha, which #3189 stopped.
	[Theory]
	[InlineData(0x00000000u)]
	[InlineData(0x00FFFFFFu)]
	[InlineData(0x80FFFFFFu)]
	[InlineData(0xFFFFFFFFu)]
	public async Task SetColorOnAndroid15AndLaterKeepsTheSystemBarBackgroundsForEveryAlpha(uint argb)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only an edge-to-edge window (Android 15 and later) paints its system bars black without DrawsSystemBarBackgrounds.");
		SetupBuilder();
		await ResetNativeStateAsync();
		await AssertEventuallyOnMainThread(static () => !AndroidSystemBars.PaintsASystemBarBlack(AndroidSystemBars.ActivityWindow), "The decor view still paints a system bar black after the reset.");
		var color = Color.FromUint(argb);

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(color));

		await AssertStatusBarShowsAsync(color);
		await InvokeOnMainThreadAsync(static () => AndroidSystemBars.FlushPostedActions(AndroidSystemBars.ActivityWindow));
		await InvokeOnMainThreadAsync(static () => Assert.False(AndroidSystemBars.PaintsASystemBarBlack(AndroidSystemBars.ActivityWindow), "The decor view paints the status bar or the navigation bar black."));
	}

	[Fact]
	public async Task SetColorKeepsTheWindowFlagsWhileSwitchingBetweenTransparentAndOpaque()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		foreach (var color in new[] { Colors.Red, Colors.Transparent, Colors.Blue, Colors.Transparent })
		{
			await InvokeOnMainThreadAsync(() => StatusBar.SetColor(color));
			await AssertStatusBarShowsAsync(color);
		}
	}

	[Fact]
	public async Task SetColorCalledAgainUpdatesTheExistingOverlay()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(Colors.Red));
		await AssertStatusBarShowsAsync(Colors.Red);
		var firstOverlay = await InvokeOnMainThreadAsync(static () => AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag));

		await InvokeOnMainThreadAsync(() => StatusBar.SetColor(Colors.Green));

		await AssertStatusBarShowsAsync(Colors.Green);
		await InvokeOnMainThreadAsync(() => Assert.Same(firstOverlay, AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag)));
	}

	[Fact]
	public async Task SetColorCalledTwiceInOneUiMessageAddsASingleOverlay()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		// Android 16 posts the overlay creation to the decor view: both calls see no overlay and the second posted action finds the first one's.
		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(Colors.Red);
			StatusBar.SetColor(Colors.Blue);
		});

		await AssertStatusBarShowsAsync(Colors.Blue);
	}

	// Android 16 posts the overlay creation to the decor view: the second posted action finds the overlay of the first (transparent) one,
	// colors it and leaves the window flags as the first action left them.
	[Fact]
	public async Task SetColorTransparentThenOpaqueInOneUiMessageEndsOpaque()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			StatusBar.SetColor(Colors.Transparent);
			StatusBar.SetColor(Colors.Red);
		});

		await AssertStatusBarShowsAsync(Colors.Red);
	}

	// Android 16 posts the overlay creation to the decor view, so a window that is not attached yet (e.g. SetColor in Activity.OnCreate) gets
	// its overlay only once it is attached and its insets are known: no overlay is sized before the window has a status bar height.
	[Fact]
	public async Task SetColorBeforeTheWindowIsAttachedCreatesTheOverlayOnlyOnceItIsAttached()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(36), "Only Android 16 (API 36) and later post the overlay creation; Android 15 creates it at once and older versions have no overlay.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var unshown = await InvokeOnMainThreadAsync(static () => new UnshownWindow());

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				Assert.Null(unshown.DecorView.RootWindowInsets);

				AndroidSystemBars.WithCurrentActivity(unshown.Activity, static () =>
				{
					StatusBar.SetColor(Colors.Red);
					return true;
				});

				Assert.Empty(AndroidSystemBars.FindTagged(unshown.Window, AndroidSystemBars.StatusBarOverlayTag));
			});

			await InvokeOnMainThreadAsync(unshown.Show);

			await AssertEventuallyOnMainThread(
				() => AndroidSystemBars.FindOverlay(unshown.Window, AndroidSystemBars.StatusBarOverlayTag) is AView overlay && AndroidSystemBars.BackgroundColor(overlay) == Colors.Red.ToPlatform(),
				"The status bar overlay was not added in red once the window was attached.");
			await InvokeOnMainThreadAsync(() =>
			{
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(unshown.Window, AndroidSystemBars.StatusBarOverlayTag));
				Assert.Equal(ExpectedOverlayHeight(unshown.Window), overlay.LayoutParameters?.Height);
				AndroidSystemBars.AssertDrawnAboveTheContent(unshown.Window, overlay);
				Assert.True(AndroidSystemBars.HasFlag(unshown.Window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));
				Assert.False(AndroidSystemBars.HasFlag(unshown.Window, Android.Views.WindowManagerFlags.LayoutNoLimits));
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(unshown.Dispose);
		}
	}

	[Fact]
	public async Task SetColorDoesNotTakeOverAnAppViewThatCarriesTheOverlayTag()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later look for an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var appView = await InvokeOnMainThreadAsync(static () => AddAppViewToTheContent(AndroidSystemBars.StatusBarOverlayTag));

		try
		{
			await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));
			await InvokeOnMainThreadAsync(static () => AndroidSystemBars.FlushPostedActions(AndroidSystemBars.ActivityWindow));

			await InvokeOnMainThreadAsync(() => Assert.Equal(Colors.Blue.ToPlatform(), AndroidSystemBars.BackgroundColor(appView)));
			await AssertStatusBarShowsAsync(Colors.Red);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => (appView.Parent as Android.Views.ViewGroup)?.RemoveView(appView));
		}
	}

	[Fact]
	public async Task SetColorWithoutCurrentActivityThrowsInvalidOperationException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => AndroidSystemBars.WithoutCurrentActivity(static () => Assert.Throws<InvalidOperationException>(static () => StatusBar.SetColor(Colors.Red))));

		Assert.Equal("Android Activity can't be null.", exception.Message);
	}

	[Fact]
	public async Task SetColorStyleAndBarSizeBeforeAndroid6DoNothing()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(23), "Needs an Android 5 (API 21-22) device: StatusBar.IsSupported is false only there (its factory traces and returns false), and the pools have API 28 and 36. The guard arms run on the pools through UnsupportedAndroidVersion.");
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var before = AndroidSystemBars.Flags(window);
			var lightStatusBarsBefore = AndroidSystemBars.IsLightStatusBars(window);
			StatusBar.SetColor(Colors.Red);
			StatusBar.SetStyle(StatusBarStyle.DarkContent);
			StatusBar.UpdateBarSize();

			Assert.Equal(before, AndroidSystemBars.Flags(window));
			Assert.Equal(lightStatusBarsBefore, AndroidSystemBars.IsLightStatusBars(window));
			Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
		});
	}

	// StatusBar.IsSupported is false only on Android 5 (API 21-22), which no pool device runs (see the skipped test above), so these tests
	// switch the support flag off (UnsupportedAndroidVersion) to reach the guard arms of SetColor, SetStyle and UpdateBarSize.
	[Fact]
	public async Task SetColorDoesNothingWhileTheStatusBarIsMarkedUnsupported()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		using (UnsupportedAndroidVersion.ForStatusBar())
		{
			await InvokeOnMainThreadAsync(static () =>
			{
				StatusBar.SetColor(Colors.Red);
				StatusBar.SetColor(Colors.Transparent);
				StatusBar.SetColor(null);
			});

			// Android 16 posts the overlay creation to the decor view: let everything that was posted run before looking.
			await InvokeOnMainThreadAsync(static () => AndroidSystemBars.FlushPostedActions(AndroidSystemBars.ActivityWindow));
		}

		await AssertStatusBarUntouchedAsync();
	}

	[Fact]
	public async Task SetStyleDoesNothingWhileTheStatusBarIsMarkedUnsupported()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		using (UnsupportedAndroidVersion.ForStatusBar())
		{
			await InvokeOnMainThreadAsync(static () =>
			{
				StatusBar.SetStyle(StatusBarStyle.DarkContent);

				// The support check comes before the style is looked at: an undefined style does not throw here.
				StatusBar.SetStyle((StatusBarStyle)42);
			});
		}

		await AssertStatusBarUntouchedAsync();
	}

	[Fact]
	public async Task UpdateBarSizeDoesNothingWhileTheStatusBarIsMarkedUnsupported()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		// An overlay with a wrong height, which UpdateBarSize would restore on Android 15+ (and would throw NotSupportedException for before).
		var overlay = await InvokeOnMainThreadAsync(static () =>
		{
			var view = new AView(AndroidSystemBars.Activity) { Tag = AndroidSystemBars.StatusBarOverlayTag, LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, 1) };
			AndroidSystemBars.Decor(AndroidSystemBars.ActivityWindow).AddView(view);
			return view;
		});

		using (UnsupportedAndroidVersion.ForStatusBar())
		{
			await InvokeOnMainThreadAsync(static () => StatusBar.UpdateBarSize());
		}

		await InvokeOnMainThreadAsync(() => Assert.Equal(1, overlay.LayoutParameters?.Height));
	}

	[Fact]
	public async Task SetColorOnAndroid15SizesTheOverlayFromTheStatusBarHeightResource()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35) && !OperatingSystem.IsAndroidVersionAtLeast(36), "Needs an Android 15 (API 35) device or emulator: the pools have API 28 and 36.");
		SetupBuilder();
		await ResetNativeStateAsync();

		// Android 15 adds the overlay synchronously (no post), so there is nothing to wait for.
		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);

			var overlay = AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag);
			Assert.NotNull(overlay);
			Assert.Equal(AndroidSystemBars.FrameworkDimension("status_bar_height") + overlayHeightPadding, overlay.LayoutParameters?.Height);
			Assert.Equal(Colors.Red.ToPlatform(), AndroidSystemBars.BackgroundColor(overlay));
		});
	}

	[Fact]
	public async Task SetColorCalledAgainOnAndroid15UpdatesTheExistingOverlay()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35) && !OperatingSystem.IsAndroidVersionAtLeast(36), "Needs an Android 15 (API 35) device or emulator: the pools have API 28 and 36.");
		SetupBuilder();
		await ResetNativeStateAsync();

		// Android 15 adds the overlay synchronously (no post), so there is nothing to wait for.
		await InvokeOnMainThreadAsync(static () =>
		{
			StatusBar.SetColor(Colors.Red);
			var overlay = AndroidSystemBars.FindOverlay(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag) ?? throw new InvalidOperationException("No status bar overlay.");

			StatusBar.SetColor(Colors.Green);

			Assert.Same(overlay, Assert.Single(AndroidSystemBars.FindTagged(AndroidSystemBars.ActivityWindow, AndroidSystemBars.StatusBarOverlayTag)));
			Assert.Equal(Colors.Green.ToPlatform(), AndroidSystemBars.BackgroundColor(overlay));
		});
	}

	// The Android 15 implementation (PlatformSetColor_AndroidApi35) only runs on an API 35 device, which the pools do not have; its body does
	// not depend on the version it runs on (it measures the status bar through GetStatusBarHeight), so Android 16 runs it through reflection.
	// The version dispatch that picks it and the status_bar_height resource keep their visible API 35 skips above.
	[Fact]
	public async Task Api35ImplementationCreatesTheOverlaySynchronouslyAndThenReusesIt()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "The overlay implementation measures the status bar, which StatusBar only does from Android 15 (API 35) on; this device is older.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var api35Implementation = typeof(StatusBar).GetMethod("PlatformSetColor_AndroidApi35", BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("StatusBar no longer has a private static PlatformSetColor_AndroidApi35(Window, Color, ViewGroup).");

		await InvokeOnMainThreadAsync(() =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var decorGroup = (Android.Views.ViewGroup)window.DecorView.RootView!;

			// Start from window flags that the implementation does not set itself: from Android 11 (API 30) on it leaves the window flags alone,
			// whatever the color.
			window.AddFlags(Android.Views.WindowManagerFlags.LayoutNoLimits);
			window.ClearFlags(Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds);

			api35Implementation.Invoke(null, [window, Colors.Red.ToPlatform(), decorGroup]);

			var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
			Assert.Equal(Colors.Red.ToPlatform(), AndroidSystemBars.BackgroundColor(overlay));
			var parameters = Assert.IsType<Android.Widget.FrameLayout.LayoutParams>(overlay.LayoutParameters);
			Assert.Equal(Android.Views.ViewGroup.LayoutParams.MatchParent, parameters.Width);
			Assert.Equal(ExpectedOverlayHeight(window), parameters.Height);
			Assert.Equal(Android.Views.GravityFlags.Top, parameters.Gravity);
			AndroidSystemBars.AssertDrawnAboveTheContent(window, overlay);
			Assert.True(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));
			Assert.False(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));

			api35Implementation.Invoke(null, [window, Colors.Transparent.ToPlatform(), decorGroup]);

			Assert.Same(overlay, Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag)));
			Assert.Equal(Colors.Transparent.ToPlatform(), AndroidSystemBars.BackgroundColor(overlay));
			Assert.True(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.LayoutNoLimits));
			Assert.False(AndroidSystemBars.HasFlag(window, Android.Views.WindowManagerFlags.DrawsSystemBarBackgrounds));
		});
	}

	[Theory]
	[InlineData(StatusBarStyle.DarkContent, true)]
	[InlineData(StatusBarStyle.LightContent, false)]
	[InlineData(StatusBarStyle.Default, false)]
	public async Task SetStyleUsesLightStatusBarsOnlyForDarkContent(StatusBarStyle style, bool expectedLightStatusBars)
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) ?? throw new InvalidOperationException("The window has no insets controller.");
			controller.AppearanceLightStatusBars = !expectedLightStatusBars;
			Assert.NotEqual(expectedLightStatusBars, AndroidSystemBars.IsLightStatusBars(window));

			StatusBar.SetStyle(style);

			Assert.Equal(expectedLightStatusBars, AndroidSystemBars.IsLightStatusBars(window));
		});
	}

	[Fact]
	public async Task SetStyleCanSwitchBetweenDarkAndLightContent()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;

			StatusBar.SetStyle(StatusBarStyle.DarkContent);
			Assert.True(AndroidSystemBars.IsLightStatusBars(window));
			StatusBar.SetStyle(StatusBarStyle.LightContent);
			Assert.False(AndroidSystemBars.IsLightStatusBars(window));
			StatusBar.SetStyle(StatusBarStyle.DarkContent);
			Assert.True(AndroidSystemBars.IsLightStatusBars(window));
			StatusBar.SetStyle(StatusBarStyle.Default);
			Assert.False(AndroidSystemBars.IsLightStatusBars(window));
		});
	}

	[Fact]
	public async Task SetStyleWithoutCurrentActivityThrowsInvalidOperationException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => AndroidSystemBars.WithoutCurrentActivity(static () => Assert.Throws<InvalidOperationException>(static () => StatusBar.SetStyle(StatusBarStyle.DarkContent))));

		Assert.Equal("Android Activity can't be null.", exception.Message);
	}

	[Fact]
	public async Task UpdateBarSizeRestoresTheOverlayHeight()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "The status bar overlay (and UpdateBarSize) exist from Android 15 (API 35); this device is older.");
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));
		await AssertStatusBarShowsAsync(Colors.Red);

		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var overlay = AndroidSystemBars.FindOverlay(window, AndroidSystemBars.StatusBarOverlayTag) ?? throw new InvalidOperationException("No status bar overlay.");
			Assert.NotNull(overlay.LayoutParameters);
			overlay.LayoutParameters.Height = 1;

			StatusBar.UpdateBarSize();

			Assert.Equal(ExpectedOverlayHeight(window), overlay.LayoutParameters.Height);
		});
	}

	// MAUI shows modal pages in dialog fragments: SetColor colors the dialog's window then, and UpdateBarSize has to size that overlay.
	[Fact]
	public async Task UpdateBarSizeResizesTheOverlayOfTheVisibleDialog()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "The status bar overlay (and UpdateBarSize) exist from Android 15 (API 35); this device is older.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(UpdateBarSizeResizesTheOverlayOfTheVisibleDialog)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));
			await AssertStatusBarShowsAsync(Colors.Red, dialog);

			await InvokeOnMainThreadAsync(() =>
			{
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(dialog.DialogWindow, AndroidSystemBars.StatusBarOverlayTag));
				Assert.NotNull(overlay.LayoutParameters);
				overlay.LayoutParameters.Height = 1;
				Assert.NotEqual(1, ExpectedOverlayHeight(dialog.DialogWindow));

				StatusBar.UpdateBarSize();

				Assert.Equal(ExpectedOverlayHeight(dialog.DialogWindow), overlay.LayoutParameters.Height);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// The toolkit's overlay exists before the app view is added, so only UpdateBarSize's own lookup of the overlay is under test (SetColor's is
	// SetColorDoesNotTakeOverAnAppViewThatCarriesTheOverlayTag).
	[Fact]
	public async Task UpdateBarSizeDoesNotResizeAnAppViewThatCarriesTheOverlayTag()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "The status bar overlay (and UpdateBarSize's lookup of it) exists from Android 15 (API 35); older versions throw for an overlay (UpdateBarSizeBeforeAndroid15ThrowsNotSupportedExceptionWhenAnOverlayExists).");
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));
		await AssertStatusBarShowsAsync(Colors.Red);
		var appView = await InvokeOnMainThreadAsync(static () => AddAppViewToTheContent(AndroidSystemBars.StatusBarOverlayTag));

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var window = AndroidSystemBars.ActivityWindow;
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
				Assert.NotNull(overlay.LayoutParameters);
				overlay.LayoutParameters.Height = 1;
				var appViewHeight = appView.LayoutParameters?.Height;
				Assert.Equal(AppViewSize, appViewHeight);
				Assert.NotEqual(AppViewSize, ExpectedOverlayHeight(window));

				StatusBar.UpdateBarSize();

				Assert.True(appView.LayoutParameters?.Height == appViewHeight, $"UpdateBarSize took the app view tagged \"StatusBarOverlay\" for the status bar overlay: it resized it from {appViewHeight} to {appView.LayoutParameters?.Height} pixels.");
				Assert.Equal(ExpectedOverlayHeight(window), overlay.LayoutParameters.Height);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => (appView.Parent as Android.Views.ViewGroup)?.RemoveView(appView));
		}
	}

	[Fact]
	public async Task UpdateBarSizeWithoutOverlayDoesNothing()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var flags = AndroidSystemBars.Flags(window);

			// Before Android 15 measuring the status bar throws NotSupportedException: without an overlay nothing is measured.
			StatusBar.UpdateBarSize();

			Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.StatusBarOverlayTag));
			Assert.Equal(flags, AndroidSystemBars.Flags(window));
		});
	}

	[Fact]
	public async Task UpdateBarSizeIgnoresAnOverlayWithoutLayoutParameters()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			using var unshown = new UnshownWindow();
			using var overlay = new AView(AndroidSystemBars.Activity) { Tag = AndroidSystemBars.StatusBarOverlayTag };
			using var root = new RootWithAChildWithoutLayoutParameters(AndroidSystemBars.Activity, unshown.DecorView, overlay);

			// The status bar code looks for its overlay among the children of the window's root view, which for a decor view that is not
			// attached to a window is its topmost parent.
			Assert.Same(root, unshown.DecorView.RootView);
			Assert.Null(overlay.LayoutParameters);

			// Before Android 15 measuring the status bar would throw NotSupportedException: the missing layout parameters must stop it first.
			AndroidSystemBars.WithCurrentActivity(unshown.Activity, static () =>
			{
				StatusBar.UpdateBarSize();
				return true;
			});

			Assert.Null(overlay.LayoutParameters);
		});
	}

	[Fact]
	public async Task UpdateBarSizeUsesNoStatusBarHeightWhileTheWindowHasNoInsets()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(36), "Android 16 (API 36) reads the status bar height from the window insets; Android 15 reads a resource (needs an API 35 device) and older versions throw.");
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(static () =>
		{
			using var unshown = new UnshownWindow();
			var overlay = new AView(AndroidSystemBars.Activity) { Tag = AndroidSystemBars.StatusBarOverlayTag, LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(1, 1) };
			unshown.DecorView.AddView(overlay);
			Assert.Null(unshown.DecorView.RootWindowInsets);

			AndroidSystemBars.WithCurrentActivity(unshown.Activity, static () =>
			{
				StatusBar.UpdateBarSize();
				return true;
			});

			Assert.Equal(overlayHeightPadding, overlay.LayoutParameters?.Height);
		});
	}

	[Fact]
	public async Task UpdateBarSizeWithoutCurrentActivityThrowsInvalidOperationException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => AndroidSystemBars.WithoutCurrentActivity(static () => Assert.Throws<InvalidOperationException>(StatusBar.UpdateBarSize)));

		Assert.Equal("Android Activity can't be null.", exception.Message);
	}

	[Fact]
	public async Task UpdateBarSizeBeforeAndroid15ThrowsNotSupportedExceptionWhenAnOverlayExists()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(35), "Android 15 and later size the overlay; only older versions throw for it.");
		SetupBuilder();
		await ResetNativeStateAsync();

		var exception = await InvokeOnMainThreadAsync(static () =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			AndroidSystemBars.Decor(window).AddView(new AView(AndroidSystemBars.Activity) { Tag = AndroidSystemBars.StatusBarOverlayTag });

			return Assert.Throws<NotSupportedException>(StatusBar.UpdateBarSize);
		});

		Assert.Equal("StatusBar overlay is only supported on Android API 35+", exception.Message);
	}

	[Fact]
	public async Task SetStyleChangesTheStatusBarOfTheVisibleDialogOnly()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(SetStyleChangesTheStatusBarOfTheVisibleDialogOnly)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");

			await InvokeOnMainThreadAsync(() =>
			{
				StatusBar.SetStyle(StatusBarStyle.DarkContent);

				Assert.True(AndroidSystemBars.IsLightStatusBars(dialog.DialogWindow));
				Assert.Equal(AndroidSystemBars.OriginalState.LightStatusBars, AndroidSystemBars.IsLightStatusBars(AndroidSystemBars.ActivityWindow));
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	[Fact]
	public async Task SetColorChangesTheStatusBarOfTheVisibleDialogOnly()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(SetColorChangesTheStatusBarOfTheVisibleDialogOnly)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			await InvokeOnMainThreadAsync(static () => StatusBar.SetColor(Colors.Red));

			await AssertStatusBarShowsAsync(Colors.Red, dialog);
			await InvokeOnMainThreadAsync(static () =>
			{
				var activityWindow = AndroidSystemBars.ActivityWindow;
				Assert.Empty(AndroidSystemBars.FindTagged(activityWindow, AndroidSystemBars.StatusBarOverlayTag));
				Assert.NotEqual(Colors.Red.ToPlatform().ToArgb(), activityWindow.StatusBarColor);
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	internal const int AppViewSize = 10;

	internal static AView AddAppViewToTheContent(string tag)
	{
		var content = AndroidSystemBars.Activity.FindViewById<Android.Widget.FrameLayout>(Android.Resource.Id.Content) ?? throw new InvalidOperationException("The activity has no content view.");
		var view = new AView(AndroidSystemBars.Activity) { Tag = tag };
		view.SetBackgroundColor(Colors.Blue.ToPlatform());
		content.AddView(view, new Android.Widget.FrameLayout.LayoutParams(AppViewSize, AppViewSize));
		return view;
	}

	// ViewGroup.addView gives a child without layout parameters default ones: only the protected attachViewToParent leaves a child without them.
	sealed class RootWithAChildWithoutLayoutParameters : Android.Widget.FrameLayout
	{
		public RootWithAChildWithoutLayoutParameters(Android.Content.Context context, AView decorView, AView childWithoutLayoutParameters) : base(context)
		{
			AddView(decorView);
			AttachViewToParent(childWithoutLayoutParameters, -1, null);
		}
	}
}