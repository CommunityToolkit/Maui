#pragma warning disable CA1422 // Window.NavigationBarColor is obsolete from API 35: the tests still read it on the older APIs that use it.
using System.Reflection;
using System.Runtime.CompilerServices;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AndroidPlatform = Microsoft.Maui.Controls.PlatformConfiguration.Android;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class NavigationBarTests
{
	// NavigationBar remembers what it changed in each window until the page whose value the window shows clears it, and ResetNativeStateAsync
	// puts the runner's window back without NavigationBar knowing: what it remembered in an earlier test would be wrong for this one, so every
	// test starts with NavigationBar remembering nothing.
	public NavigationBarTests() => ForgetWindowChanges();

	// What NavigationBar does per Android version:
	//   API < 23   unsupported: both mappings return before touching the window
	//   API 23-34  Color sets the window's navigation bar color
	//   API 35+    Color adds (once) a tagged overlay view to the decor view, over the window's navigation bar: at the bottom, or with
	//              three-button navigation in landscape at the right or left edge. It follows the bar whenever the window dispatches its insets.
	// Style turns the light navigation bar icons on or off on every supported version. A page that clears its Color or Style (ClearValue) gives
	// every window that still shows its value back what the window had before NavigationBar first changed it: the navigation bar color (from
	// API 35 on the overlay goes away) or the light appearance. The pools provide API 28 and 36.

	[Fact]
	public async Task RemapForControlsRegistersTheColorAndStyleMappingsOfThePageHandler()
	{
		SetupBuilder();

		var keys = PageHandler.Mapper.GetKeys().ToList();

		Assert.Contains(NavigationBar.ColorProperty.PropertyName, keys);
		Assert.Contains(NavigationBar.StyleProperty.PropertyName, keys);
	}

	[Theory]
	[InlineData(0xFFFF0000u)]
	[InlineData(0xFF0000FFu)]
	[InlineData(0x80336699u)]
	[InlineData(0x00000000u)]
	public async Task ColorSetBeforeThePageHandlerIsCreatedShowsInTheNavigationBar(uint argb)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var color = Color.FromUint(argb);
		var page = CreatePage();
		NavigationBar.SetColor(page, color);
		await AssertNavigationBarUntouchedAsync();

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await AssertNavigationBarShowsAsync(color);
	}

	[Fact]
	public async Task ColorSetThroughThePlatformConfigurationShowsInTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarUntouchedAsync();

		await InvokeOnMainThreadAsync(() => page.On<AndroidPlatform>().SetColor(Colors.Green));

		await AssertNavigationBarShowsAsync(Colors.Green);
		await InvokeOnMainThreadAsync(() => Assert.Equal(Colors.Green, page.On<AndroidPlatform>().GetColor()));
	}

	[Fact]
	public async Task ColorChangedAfterThePageHandlerWasCreatedUpdatesTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarShowsAsync(Colors.Red);

		await InvokeOnMainThreadAsync(() => NavigationBar.SetColor(page, Colors.Blue));
		await AssertNavigationBarShowsAsync(Colors.Blue);

		await InvokeOnMainThreadAsync(() => NavigationBar.SetColor(page, Colors.Transparent));
		await AssertNavigationBarShowsAsync(Colors.Transparent);
	}

	[Fact]
	public async Task ColorChangedOnAnotherPageReusesTheNavigationBarOverlay()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var firstPage = CreatePage();
		var secondPage = CreatePage();
		NavigationBar.SetColor(firstPage, Colors.Red);
		NavigationBar.SetColor(secondPage, Colors.Green);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(firstPage));
		await AssertNavigationBarShowsAsync(Colors.Red);

		// API 35+: a second page finds the overlay of the first one and only changes its color (AssertNavigationBarShowsAsync
		// asserts that exactly one overlay exists). Before: the window's navigation bar color changes.
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(secondPage));

		await AssertNavigationBarShowsAsync(Colors.Green);
	}

	[Fact]
	public async Task ColorNotSetLeavesTheNavigationBarAlone()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(CreatePage()));

		await AssertNavigationBarUntouchedAsync();
	}

	[Fact]
	public async Task ColorReadBeforeThePageHandlerIsCreatedDoesNotChangeTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();

		// Only reading the color, never setting it: the app did not ask for a navigation bar color.
		Assert.Equal(Color.FromUint(0u), NavigationBar.GetColor(page));
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await AssertNavigationBarUntouchedAsync();
	}

	// A page that clears its Color no longer asks for that color, so the navigation bar stops showing it: the window gets back the navigation
	// bar it had before NavigationBar's first change (ColorClearedAfterItWasChangedRestoresTheNavigationBarTheWindowHadBeforeTheFirstChange pins
	// which one). The never-set case of the same mapping is ColorNotSetLeavesTheNavigationBarAlone.
	[Fact]
	public async Task ColorClearedAfterItWasAppliedNoLongerShowsInTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarShowsAsync(Colors.Red);

		await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.ColorProperty));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.False(page.IsSet(NavigationBar.ColorProperty));
			var window = AndroidSystemBars.ActivityWindow;
			var red = Colors.Red.ToPlatform();
			Assert.False(
				AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag).Any(overlay => AndroidSystemBars.BackgroundColor(overlay) == red),
				"The navigation bar overlay still shows the cleared color.");
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				Assert.True(window.NavigationBarColor != red.ToArgb(), "The window's navigation bar still has the cleared color.");
			}
		});
	}

	// The navigation bar that comes back is the one the window had before NavigationBar's first change: not the property's default, and not an
	// earlier color of the page. Below API 35 that is the window's own navigation bar color; from API 35 on the overlay is the color, and the
	// window had none of its own. A last color equal to the property's default (0x00000000) is cleared without any PropertyChanged.
	[Theory]
	[InlineData(0xFF0000FFu)]
	[InlineData(0x00000000u)]
	public async Task ColorClearedAfterItWasChangedRestoresTheNavigationBarTheWindowHadBeforeTheFirstChange(uint lastArgb)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var windowColor = Color.FromUint(0xFF123456);
		var lastColor = Color.FromUint(lastArgb);
		await InvokeOnMainThreadAsync(() => SetWindowNavigationBarColor(windowColor));
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await InvokeOnMainThreadAsync(() => NavigationBar.SetColor(page, lastColor));
		await AssertNavigationBarShowsAsync(lastColor);

		await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.ColorProperty));

		await AssertOwnNavigationBarAsync(windowColor.ToPlatform().ToArgb());
	}

	// A page that cleared its Color before NavigationBar applied it changed no window, so it has nothing to give back: the window keeps the color
	// of another page (both the clear and the creation of the page's handler reach NavigationBar).
	[Fact]
	public async Task ColorClearedBeforeItWasAppliedKeepsTheColorOfAnotherPage()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var coloredPage = CreatePage();
		NavigationBar.SetColor(coloredPage, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(coloredPage));
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Blue);
		page.ClearValue(NavigationBar.ColorProperty);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		Assert.False(page.IsSet(NavigationBar.ColorProperty));
		await AssertNavigationBarShowsAsync(Colors.Red);
	}

	// The window shows the color of the page that NavigationBar applied last. A page whose color another page replaced has nothing left to give
	// back when it clears it; once the other page clears its color too, the window gets back the navigation bar it had before the first page's.
	[Fact]
	public async Task ColorClearedAfterAnotherPageReplacedItKeepsTheColorOfTheOtherPage()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var firstPage = CreatePage();
		var secondPage = CreatePage();
		NavigationBar.SetColor(firstPage, Colors.Red);
		NavigationBar.SetColor(secondPage, Colors.Green);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(firstPage));
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(secondPage));

		await InvokeOnMainThreadAsync(() => firstPage.ClearValue(NavigationBar.ColorProperty));
		await AssertNavigationBarShowsAsync(Colors.Green);

		await InvokeOnMainThreadAsync(() => secondPage.ClearValue(NavigationBar.ColorProperty));
		await AssertNavigationBarUntouchedAsync();
	}

	// NavigationBar changes the current window: the activity's, or a visible dialog's. When the page clears its color, every window that shows
	// one of its colors gets back its own navigation bar, also the activity's window while the dialog's is the current one.
	[Fact]
	public async Task ColorClearedGivesEveryWindowThatShowsItBackItsOwnNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(ColorClearedGivesEveryWindowThatShowsItBackItsOwnNavigationBar)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			var dialogNavigationBarColor = await InvokeOnMainThreadAsync(() => dialog.DialogWindow.NavigationBarColor);
			await InvokeOnMainThreadAsync(() => NavigationBar.SetColor(page, Colors.Green));
			await AssertNavigationBarShowsAsync(Colors.Green, dialog);
			await AssertNavigationBarShowsAsync(Colors.Red);

			await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.ColorProperty));

			await AssertOwnNavigationBarAsync(dialogNavigationBarColor, dialog);
			await AssertNavigationBarUntouchedAsync();
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// NavigationBar remembers the windows it changed without keeping them alive: a garbage collection between the change and the clear must not
	// make it forget the runner's window, which is still alive (its .NET object can be collected and created again).
	[Fact]
	public async Task ColorClearedAfterAGarbageCollectionStillGivesTheWindowBackItsNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarShowsAsync(Colors.Red);

		await WaitForGC();
		await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.ColorProperty));

		await AssertNavigationBarUntouchedAsync();
	}

	// NavigationBar remembers which page's value a window shows without keeping the page alive.
	[Fact]
	public async Task PageWhoseColorAndStyleWereAppliedIsCollected()
	{
		SetupBuilder();
		await ResetNativeStateAsync();

		var pageReference = await ApplyColorAndStyleOfANewPageAsync();

		await AssertionExtensions.WaitForGC(pageReference);
	}

	[Fact]
	public async Task ColorDoesNotTakeOverAnAppViewThatCarriesTheOverlayTag()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later look for an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var appView = await InvokeOnMainThreadAsync(static () => StatusBarTests.AddAppViewToTheContent(AndroidSystemBars.NavigationBarOverlayTag));
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);

		try
		{
			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

			// The app's view keeps its own color, and the navigation bar gets an overlay of its own.
			await InvokeOnMainThreadAsync(() => Assert.Equal(Colors.Blue.ToPlatform(), AndroidSystemBars.BackgroundColor(appView)));
			await AssertNavigationBarShowsAsync(Colors.Red);
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => (appView.Parent as Android.Views.ViewGroup)?.RemoveView(appView));
		}
	}

	// From API 35 on the overlay is the navigation bar color: it covers exactly the navigation bar that the window reports in its insets (48 dp
	// high with three-button navigation, 24 dp with gesture navigation), and nothing of the page above it.
	[Fact]
	public async Task ColorOverlayCoversExactlyTheNavigationBarOfTheWindow()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later show the color in an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await AssertNavigationBarOverlayCoversTheNavigationBarAsync();
	}

	// Where the overlay goes for the navigation bar insets of a window, as the decor view places its own navigation bar background: at the bottom,
	// or, when the navigation bar has no bottom inset, at the right or left edge (three-button navigation in landscape or reverse landscape). The
	// pools' emulators use gesture navigation, so the side edges are only reached with insets made for the test.
	[Theory]
	[InlineData(0, 0, 0, 126, Android.Views.ViewGroup.LayoutParams.MatchParent, 126, Android.Views.GravityFlags.Bottom)] // three-button navigation
	[InlineData(0, 0, 0, 63, Android.Views.ViewGroup.LayoutParams.MatchParent, 63, Android.Views.GravityFlags.Bottom)] // gesture navigation
	[InlineData(0, 0, 126, 0, 126, Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.GravityFlags.Right)] // landscape
	[InlineData(126, 0, 0, 0, 126, Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.GravityFlags.Left)] // reverse landscape
	[InlineData(0, 0, 126, 63, Android.Views.ViewGroup.LayoutParams.MatchParent, 63, Android.Views.GravityFlags.Bottom)] // a bottom inset wins
	[InlineData(0, 0, 0, 0, Android.Views.ViewGroup.LayoutParams.MatchParent, 0, Android.Views.GravityFlags.Bottom)] // no navigation bar
	public async Task ColorOverlayIsLaidOutAtTheEdgeOfTheNavigationBarInsets(int left, int top, int right, int bottom, int expectedWidth, int expectedHeight, Android.Views.GravityFlags expectedGravity)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later show the color in an overlay view; older versions color the window.");
		SetupBuilder();

		var layout = await InvokeOnMainThreadAsync(() =>
		{
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				throw new PlatformNotSupportedException("The navigation bar overlay exists from API 35 on.");
			}

			var overlay = new Android.Views.View(AndroidSystemBars.Activity);
			var insets = new Android.Views.WindowInsets.Builder()
				.SetInsets(Android.Views.WindowInsets.Type.NavigationBars(), Android.Graphics.Insets.Of(left, top, right, bottom))
				.Build();

			NavigationBar.UpdateNavigationBarOverlayLayout(overlay, insets);

			var parameters = Assert.IsType<Android.Widget.FrameLayout.LayoutParams>(overlay.LayoutParameters);
			return (parameters.Width, parameters.Height, parameters.Gravity);
		});

		Assert.Equal((expectedWidth, expectedHeight, expectedGravity), layout);
	}

	// The window dispatches its insets again whenever its navigation bar moves or changes size (the window rotates, the navigation bar is hidden
	// or shown), and the overlay is laid out over the navigation bar again each time.
	[Fact]
	public async Task ColorOverlayIsLaidOutOverTheNavigationBarAgainWhenTheWindowDispatchesItsInsets()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later show the color in an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await InvokeOnMainThreadAsync(static () =>
		{
			// Somewhere the navigation bar is not: along the top edge of the window
			var window = AndroidSystemBars.ActivityWindow;
			var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
			overlay.LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, 1) { Gravity = Android.Views.GravityFlags.Top };

			window.DecorView.RequestApplyInsets();
		});

		await AssertNavigationBarOverlayCoversTheNavigationBarAsync();
	}

	// With three-button navigation, Android moves the navigation bar to the right edge in landscape and to the left edge in reverse landscape;
	// with gesture navigation it stays at the bottom. The runner's activity handles orientation changes itself, as the activity of a .NET MAUI app
	// does: the window stays and dispatches its insets again.
	[Fact]
	public async Task ColorOverlayFollowsTheNavigationBarWhenTheWindowRotates()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later show the color in an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarOverlayCoversTheNavigationBarAsync();

		var activity = AndroidSystemBars.Activity;
		var (requestedOrientation, wasPortrait) = await InvokeOnMainThreadAsync(() => (activity.RequestedOrientation, IsPortrait(activity)));

		try
		{
			await InvokeOnMainThreadAsync(() => activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Landscape);
			await AssertEventuallyOnMainThread(() => IsLandscape(activity), "The window did not turn to landscape.");
			await AssertNavigationBarOverlayCoversTheNavigationBarAsync();

			var landscapeRotation = await InvokeOnMainThreadAsync(() => DisplayRotation(activity));
			await InvokeOnMainThreadAsync(() => activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.ReverseLandscape);
			await AssertEventuallyOnMainThread(() => IsLandscape(activity) && DisplayRotation(activity) != landscapeRotation, "The window did not turn to reverse landscape.");
			await AssertNavigationBarOverlayCoversTheNavigationBarAsync();

			await InvokeOnMainThreadAsync(() => activity.RequestedOrientation = Android.Content.PM.ScreenOrientation.Portrait);
			await AssertEventuallyOnMainThread(() => IsPortrait(activity), "The window did not turn back to portrait.");
			await AssertNavigationBarOverlayCoversTheNavigationBarAsync();
		}
		finally
		{
			// The tests after this one expect the window in the orientation it had
			await InvokeOnMainThreadAsync(() => activity.RequestedOrientation = requestedOrientation);
			await WaitOnMainThreadAsync(() => IsPortrait(activity) == wasPortrait);
		}

		await InvokeOnMainThreadAsync(() => Assert.Equal(wasPortrait, IsPortrait(activity)));
	}

	// The decor view adds its own navigation bar background (behind three-button navigation, a scrim of the window's background color) when the
	// window first dispatches its insets: after the overlay when a page applies its color before that, as the first page of an app or of an activity
	// that Android recreated (another font size, another navigation mode) does. The overlay stays above it, and casts no shadow.
	[Fact]
	public async Task ColorOverlayIsDrawnAboveAViewThatTheDecorViewAddsAfterIt()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Only Android 15 and later show the color in an overlay view; older versions color the window.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		var laterChild = await InvokeOnMainThreadAsync(static () =>
		{
			var child = new Android.Views.View(AndroidSystemBars.Activity)
			{
				LayoutParameters = new Android.Widget.FrameLayout.LayoutParams(Android.Views.ViewGroup.LayoutParams.MatchParent, Android.Views.ViewGroup.LayoutParams.MatchParent)
			};
			child.SetBackgroundColor(Colors.Blue.ToPlatform());
			AndroidSystemBars.Decor(AndroidSystemBars.ActivityWindow).AddView(child);
			return child;
		});

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				var window = AndroidSystemBars.ActivityWindow;
				var overlay = Assert.Single(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
				AndroidSystemBars.AssertDrawnAbove(window, overlay, laterChild);
				Assert.True(overlay.GetZ() is 0f || overlay.OutlineProvider is null, "The overlay casts a shadow: it has an elevation and an outline.");
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => (laterChild.Parent as Android.Views.ViewGroup)?.RemoveView(laterChild));
		}
	}

	[Fact]
	public async Task ColorMappingOfAViewThatIsNotAPageDoesNothing()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var pageHandler = await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(CreatePage()));
		var contentView = new ContentView();
		NavigationBar.SetColor(contentView, Colors.Red);

		await InvokeOnMainThreadAsync(() => NavigationBar.MapNavigationColorProperty(pageHandler, contentView));

		await AssertNavigationBarUntouchedAsync();
	}

	[Fact]
	public async Task ColorMappingWithoutPageHandlerThrowsNullReferenceException()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);

		await InvokeOnMainThreadAsync(() => Assert.Throws<NullReferenceException>(() => NavigationBar.MapNavigationColorProperty(null!, page)));

		await AssertNavigationBarUntouchedAsync();
	}

	[Fact]
	public async Task ColorMappingOfAPageWhoseContextIsNotAnActivityDoesNotThrowInvalidCastException()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage(); // has no handler, so setting the property does not run the mapping by itself
		NavigationBar.SetColor(page, Colors.Red);

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreatePageHandlerWithApplicationContext();

			// Whether it finds the activity behind the context or fails with a message, a failed cast is not the expected failure.
			var exception = Record.Exception(() => NavigationBar.MapNavigationColorProperty(handler, page));

			Assert.IsNotType<InvalidCastException>(exception);
		});
	}

	[Fact]
	public async Task ColorMappingOfAPageWhoseContextWrapsTheActivityShowsInTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage(); // has no handler, so setting the property does not run the mapping by itself
		NavigationBar.SetColor(page, Colors.Red);

		await InvokeOnMainThreadAsync(() => NavigationBar.MapNavigationColorProperty(CreatePageHandlerWithActivityWrapperContext(), page));

		await AssertNavigationBarShowsAsync(Colors.Red);
	}

	[Fact]
	public async Task ColorBeforeAndroid6DoesNothing()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(23), "Needs an Android 5 (API 21-22) device: NavigationBar.IsSupported is false only there, and the pools have API 28 and 36.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await AssertNavigationBarUntouchedAsync();
	}

	// NavigationBar.IsSupported is false only on Android 5 (API 21-22), which no pool device runs (see the skipped tests). These tests switch the
	// support flag off (UnsupportedAndroidVersion) and run the guard arm of the mapping: the window has to stay as ResetNativeStateAsync left it.
	[Fact]
	public async Task ColorIsNotAppliedWhileTheNavigationBarIsMarkedUnsupported()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);

		using (UnsupportedAndroidVersion.ForNavigationBar())
		{
			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		}

		await AssertNavigationBarUntouchedAsync();
	}

	[Fact]
	public async Task ColorIsShownInTheVisibleDialogOnly()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(ColorIsShownInTheVisibleDialogOnly)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			var page = CreatePage();
			NavigationBar.SetColor(page, Colors.Red);

			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

			await AssertNavigationBarShowsAsync(Colors.Red, dialog);
			await AssertNavigationBarUntouchedAsync();
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	[Theory]
	[InlineData(NavigationBarStyle.DarkContent, true)]
	[InlineData(NavigationBarStyle.LightContent, false)]
	[InlineData(NavigationBarStyle.Default, false)]
	public async Task StyleSetBeforeThePageHandlerIsCreatedUsesLightNavigationBarsOnlyForDarkContent(NavigationBarStyle style, bool expectedLightNavigationBars)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(() => SetLightNavigationBars(!expectedLightNavigationBars));
		var page = CreatePage();
		NavigationBar.SetStyle(page, style);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await InvokeOnMainThreadAsync(() => Assert.Equal(expectedLightNavigationBars, AndroidSystemBars.IsLightNavigationBars(AndroidSystemBars.ActivityWindow)));
	}

	[Fact]
	public async Task StyleSetThroughThePlatformConfigurationUpdatesTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);

		await InvokeOnMainThreadAsync(() => page.On<AndroidPlatform>().SetStyle(NavigationBarStyle.DarkContent));

		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);
		await InvokeOnMainThreadAsync(() => Assert.Equal(NavigationBarStyle.DarkContent, page.On<AndroidPlatform>().GetStyle()));
	}

	[Fact]
	public async Task StyleChangedAfterThePageHandlerWasCreatedUpdatesTheNavigationBar()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => NavigationBar.SetStyle(page, NavigationBarStyle.LightContent));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.LightContent);

		await InvokeOnMainThreadAsync(() => NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => NavigationBar.SetStyle(page, NavigationBarStyle.Default));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	[Fact]
	public async Task StyleNotSetLeavesTheNavigationBarAlone()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => SetLightNavigationBars(true));

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(CreatePage()));

		await InvokeOnMainThreadAsync(static () => Assert.True(AndroidSystemBars.IsLightNavigationBars(AndroidSystemBars.ActivityWindow)));
	}

	[Fact]
	public async Task StyleMappingOfAViewThatIsNotAPageDoesNothing()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var pageHandler = await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(CreatePage()));
		var contentView = new ContentView();
		NavigationBar.SetStyle(contentView, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => NavigationBar.MapNavigationStyleProperty(pageHandler, contentView));

		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	[Fact]
	public async Task StyleMappingWithUndefinedStyleThrowsNotSupportedException()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var handler = await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(CreatePage()));
		var page = CreatePage(); // has no handler, so setting the property does not run the mapping by itself
		NavigationBar.SetStyle(page, (NavigationBarStyle)42);

		var exception = await InvokeOnMainThreadAsync(() => Assert.Throws<NotSupportedException>(() => NavigationBar.MapNavigationStyleProperty(handler, page)));

		Assert.Equal("NavigationBar 42 is not yet supported", exception.Message);
		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	[Fact]
	public async Task StyleMappingWithoutPageHandlerThrowsNullReferenceException()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => Assert.Throws<NullReferenceException>(() => NavigationBar.MapNavigationStyleProperty(null!, page)));

		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	[Fact]
	public async Task StyleMappingOfAPageWhoseContextIsNotAnActivityDoesNotThrowInvalidCastException()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage(); // has no handler, so setting the property does not run the mapping by itself
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreatePageHandlerWithApplicationContext();

			var exception = Record.Exception(() => NavigationBar.MapNavigationStyleProperty(handler, page));

			Assert.IsNotType<InvalidCastException>(exception);
		});
	}

	[Fact]
	public async Task StyleMappingOfAPageWhoseContextWrapsTheActivityUsesItsStyle()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage(); // has no handler, so setting the property does not run the mapping by itself
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => NavigationBar.MapNavigationStyleProperty(CreatePageHandlerWithActivityWrapperContext(), page));

		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);
	}

	[Fact]
	public async Task StyleBeforeAndroid6DoesNothing()
	{
		Skip.If(OperatingSystem.IsAndroidVersionAtLeast(23), "Needs an Android 5 (API 21-22) device: NavigationBar.IsSupported is false only there, and the pools have API 28 and 36.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	[Fact]
	public async Task StyleIsNotAppliedWhileTheNavigationBarIsMarkedUnsupported()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		using (UnsupportedAndroidVersion.ForNavigationBar())
		{
			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		}

		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	// Like ColorClearedAfterItWasAppliedNoLongerShowsInTheNavigationBar: once the page cleared its Style it reports the Default style, and the
	// window no longer keeps DarkContent's light navigation bar icons. It gets back the appearance it had before NavigationBar's first change,
	// which here has no light icons, as the Default style (StyleClearedAfterItWasAppliedRestoresTheAppearanceTheWindowHadBeforeTheFirstChange
	// tells the two apart). The never-set case is StyleNotSetLeavesTheNavigationBarAlone.
	[Fact]
	public async Task StyleClearedAfterItWasAppliedNoLongerUsesLightNavigationBars()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => SetLightNavigationBars(false));
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.StyleProperty));

		Assert.False(page.IsSet(NavigationBar.StyleProperty));
		Assert.Equal(NavigationBarStyle.Default, NavigationBar.GetStyle(page));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
	}

	// The appearance that comes back is the window's own from before NavigationBar's first change, which can differ from what the Default
	// style applies (no light icons). Clearing the Default style, the property's default, raises no PropertyChanged.
	[Theory]
	[InlineData(true, NavigationBarStyle.LightContent)]
	[InlineData(true, NavigationBarStyle.Default)]
	[InlineData(false, NavigationBarStyle.DarkContent)]
	public async Task StyleClearedAfterItWasAppliedRestoresTheAppearanceTheWindowHadBeforeTheFirstChange(bool windowLightNavigationBars, NavigationBarStyle style)
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(() => SetLightNavigationBars(windowLightNavigationBars));
		var page = CreatePage();
		NavigationBar.SetStyle(page, style);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		await AssertNavigationBarStyleAsync(style);

		await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.StyleProperty));

		await InvokeOnMainThreadAsync(() => Assert.Equal(windowLightNavigationBars, AndroidSystemBars.IsLightNavigationBars(AndroidSystemBars.ActivityWindow)));
	}

	// A page that cleared its Style before NavigationBar applied it changed no window, so it has nothing to give back: the window keeps the
	// style of another page (both the clear and the creation of the page's handler reach NavigationBar).
	[Fact]
	public async Task StyleClearedBeforeItWasAppliedKeepsTheStyleOfAnotherPage()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => SetLightNavigationBars(false));
		var styledPage = CreatePage();
		NavigationBar.SetStyle(styledPage, NavigationBarStyle.DarkContent);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(styledPage));
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
		page.ClearValue(NavigationBar.StyleProperty);

		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		Assert.False(page.IsSet(NavigationBar.StyleProperty));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);
	}

	// Like ColorClearedAfterAnotherPageReplacedItKeepsTheColorOfTheOtherPage: only the page whose style the window shows gives the window back
	// its appearance.
	[Fact]
	public async Task StyleClearedAfterAnotherPageReplacedItKeepsTheStyleOfTheOtherPage()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => SetLightNavigationBars(false));
		var firstPage = CreatePage();
		var secondPage = CreatePage();
		NavigationBar.SetStyle(firstPage, NavigationBarStyle.LightContent);
		NavigationBar.SetStyle(secondPage, NavigationBarStyle.DarkContent);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(firstPage));
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(secondPage));

		await InvokeOnMainThreadAsync(() => firstPage.ClearValue(NavigationBar.StyleProperty));
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() => secondPage.ClearValue(NavigationBar.StyleProperty));
		await InvokeOnMainThreadAsync(static () => Assert.False(AndroidSystemBars.IsLightNavigationBars(AndroidSystemBars.ActivityWindow)));
	}

	// Like ColorClearedGivesEveryWindowThatShowsItBackItsOwnNavigationBar: the activity's window gets back its light icons and the dialog's window
	// its dark ones.
	[Fact]
	public async Task StyleClearedGivesEveryWindowThatShowsItBackItsOwnAppearance()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		await InvokeOnMainThreadAsync(static () => SetLightNavigationBars(true));
		var page = CreatePage();
		NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
		await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(StyleClearedGivesEveryWindowThatShowsItBackItsOwnAppearance)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			await AssertNavigationBarUntouchedAsync(dialog);
			await InvokeOnMainThreadAsync(() => NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent));
			await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent, dialog);
			await AssertNavigationBarStyleAsync(NavigationBarStyle.LightContent);

			await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.StyleProperty));

			await AssertNavigationBarUntouchedAsync(dialog);
			await InvokeOnMainThreadAsync(static () => Assert.True(AndroidSystemBars.IsLightNavigationBars(AndroidSystemBars.ActivityWindow)));
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	[Fact]
	public async Task StyleIsAppliedToTheVisibleDialogOnly()
	{
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await InvokeOnMainThreadAsync(static () => TestDialogFragment.Show(nameof(StyleIsAppliedToTheVisibleDialogOnly)));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			var page = CreatePage();
			NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

			await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent, dialog);
			await AssertNavigationBarStyleAsync(NavigationBarStyle.Default);
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// Android 15+ forces light navigation bars (dark icons) on an edge-to-edge window that has no navigation bar color of its own and a light
	// background, and then ignores the light navigation bars that the app asks for. Style takes the navigation bars back from the system: each
	// style shows, not only the dark icons that the system forces.
	[Theory]
	[InlineData(NavigationBarStyle.Default)]
	[InlineData(NavigationBarStyle.LightContent)]
	[InlineData(NavigationBarStyle.DarkContent)]
	public async Task StyleShowsOnAWindowWhoseLightBackgroundMakesTheSystemForceLightNavigationBars(NavigationBarStyle style)
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Android forces light navigation bars from Android 15 on.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await ShowDialogWithForcedLightNavigationBarsAsync($"{nameof(StyleShowsOnAWindowWhoseLightBackgroundMakesTheSystemForceLightNavigationBars)}{style}");

		try
		{
			var page = CreatePage();
			NavigationBar.SetStyle(page, style);

			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

			await AssertNavigationBarStyleAsync(style, dialog);
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// A page that clears its Style gives the window back the light navigation bars that the system forced, with the navigation bar color that Style
	// replaced to stop them.
	[Fact]
	public async Task StyleClearedGivesTheWindowBackTheLightNavigationBarsThatTheSystemForced()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Android forces light navigation bars from Android 15 on.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await ShowDialogWithForcedLightNavigationBarsAsync(nameof(StyleClearedGivesTheWindowBackTheLightNavigationBarsThatTheSystemForced));

		try
		{
			var page = CreatePage();
			NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
			await AssertNavigationBarStyleAsync(NavigationBarStyle.LightContent, dialog);

			await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.StyleProperty));

			await InvokeOnMainThreadAsync(() =>
			{
				Assert.True(AndroidSystemBars.IsForcedLightNavigationBars(dialog.DialogWindow), "The system no longer forces light navigation bars on the window.");
				Assert.False(AndroidSystemBars.IsLightNavigationBars(dialog.DialogWindow));
			});
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// The system forces light navigation bars for a background color only, and a window keeps them when its background becomes another drawable.
	// Such a window has no navigation bar color to get back when a page clears its Style, only the forced light navigation bars, and the
	// transparent navigation bar color that Style gave it stays: a Style applied again still takes the navigation bars back from the system.
	[Fact]
	public async Task StyleAppliedAgainAfterItWasClearedShowsOnAWindowWhoseBackgroundIsNotAColor()
	{
		Skip.IfNot(OperatingSystem.IsAndroidVersionAtLeast(35), "Android forces light navigation bars from Android 15 on.");
		SetupBuilder();
		await ResetNativeStateAsync();
		var dialog = await ShowDialogWithForcedLightNavigationBarsAsync(nameof(StyleAppliedAgainAfterItWasClearedShowsOnAWindowWhoseBackgroundIsNotAColor));

		try
		{
			await InvokeOnMainThreadAsync(() =>
			{
				dialog.DialogWindow.SetBackgroundDrawable(new Android.Graphics.Drawables.GradientDrawable());
				Assert.True(AndroidSystemBars.IsForcedLightNavigationBars(dialog.DialogWindow), "The window no longer has the light navigation bars that the system forced for its background color.");
			});
			var page = CreatePage();
			NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
			await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));
			await AssertNavigationBarStyleAsync(NavigationBarStyle.LightContent, dialog);
			await InvokeOnMainThreadAsync(() => page.ClearValue(NavigationBar.StyleProperty));
			await InvokeOnMainThreadAsync(() => Assert.True(AndroidSystemBars.IsForcedLightNavigationBars(dialog.DialogWindow), "The system no longer forces light navigation bars on the window."));

			await InvokeOnMainThreadAsync(() => NavigationBar.SetStyle(page, NavigationBarStyle.LightContent));

			await AssertNavigationBarStyleAsync(NavigationBarStyle.LightContent, dialog);
		}
		finally
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
		}
	}

	// A page handler whose platform views were created with the application context: their Context is not an Activity.
	PageHandler CreatePageHandlerWithApplicationContext()
	{
		var applicationContext = new MauiContext(ApplicationServices, Platform.CurrentActivity?.ApplicationContext ?? throw new InvalidOperationException("The test needs the runner's current activity."));
		var handler = CreateHandler<PageHandler>(CreatePage(), applicationContext);
		Assert.IsNotType<Android.App.Activity>(handler.PlatformView.Context, exactMatch: false);
		return handler;
	}

	// A page handler whose platform views were created with a ContextThemeWrapper around the runner's activity: their Context is not an
	// Activity, but it leads to one.
	PageHandler CreatePageHandlerWithActivityWrapperContext()
	{
		var activity = Platform.CurrentActivity ?? throw new InvalidOperationException("The test needs the runner's current activity.");
		var wrapperContext = new MauiContext(ApplicationServices, new Android.Views.ContextThemeWrapper(activity, activity.Theme));
		var handler = CreateHandler<PageHandler>(CreatePage(), wrapperContext);
		Assert.IsNotType<Android.App.Activity>(handler.PlatformView.Context, exactMatch: false);
		return handler;
	}

	static void SetLightNavigationBars(bool isLight)
	{
		var window = AndroidSystemBars.ActivityWindow;
		var controller = AndroidX.Core.View.WindowCompat.GetInsetsController(window, window.DecorView) ?? throw new InvalidOperationException("The window has no insets controller.");
		controller.AppearanceLightNavigationBars = isLight;
		Assert.Equal(isLight, AndroidSystemBars.IsLightNavigationBars(window));
	}

	// Gives the runner's window a navigation bar color of its own. From API 35 on the window ignores it (NavigationBar shows its color in an
	// overlay instead), so only the older versions get one.
	static void SetWindowNavigationBarColor(Color color)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(35))
		{
			var window = AndroidSystemBars.ActivityWindow;
			window.SetNavigationBarColor(color.ToPlatform());
			Assert.Equal(color.ToPlatform().ToArgb(), window.NavigationBarColor);
		}
	}

	// Shows a dialog and gives its window a light background. The window is new, so it has no navigation bar color of its own yet, and Android 15+
	// forces light navigation bars on it when it enforces edge-to-edge for the app (Android 16 for an app that targets it, Android 15 for an app
	// that does not opt out). Skips the test where Android does not.
	async Task<TestDialogFragment> ShowDialogWithForcedLightNavigationBarsAsync(string tag)
	{
		var dialog = await InvokeOnMainThreadAsync(() => TestDialogFragment.Show(tag));

		try
		{
			await AssertEventuallyOnMainThread(() => dialog.IsShowingAndVisible, "The dialog fragment did not become visible.");
			var isForcedLight = await InvokeOnMainThreadAsync(() =>
			{
				dialog.DialogWindow.SetBackgroundDrawable(new Android.Graphics.Drawables.ColorDrawable(Android.Graphics.Color.White));
				return AndroidSystemBars.IsForcedLightNavigationBars(dialog.DialogWindow);
			});
			Skip.IfNot(isForcedLight, "Android does not force light navigation bars on this window: it does not enforce edge-to-edge for the app on this device.");

			return dialog;
		}
		catch
		{
			await InvokeOnMainThreadAsync(dialog.Remove);
			throw;
		}
	}

	// Waits until the navigation bar overlay of the runner's window is laid out over the window's navigation bar (the decor view lays its children
	// out after the window dispatched its insets), and fails with both rectangles when it does not get there.
	async Task AssertNavigationBarOverlayCoversTheNavigationBarAsync()
	{
		var frames = string.Empty;
		var coversTheNavigationBar = await WaitOnMainThreadAsync(() =>
		{
			var window = AndroidSystemBars.ActivityWindow;
			var navigationBar = AndroidSystemBars.NavigationBarFrame(window);
			var overlay = AndroidSystemBars.FindOverlay(window, AndroidSystemBars.NavigationBarOverlayTag);
			var overlayFrame = overlay is null ? null : AndroidSystemBars.Frame(overlay);
			frames = $"overlay {overlayFrame?.ToShortString() ?? "missing"}, navigation bar {navigationBar.ToShortString()}, decor view {window.DecorView.Width}x{window.DecorView.Height}";
			return navigationBar.Equals(overlayFrame);
		});

		Assert.True(coversTheNavigationBar, $"The navigation bar overlay does not cover the window's navigation bar: {frames}.");
	}

	// Polls the condition on the UI thread like AssertEventuallyOnMainThread, but reports a timeout as false instead of failing: for clean-up code
	// and for assertions that explain the failure themselves.
	async Task<bool> WaitOnMainThreadAsync(Func<bool> condition, int timeout = 10000)
	{
		try
		{
			await AssertEventuallyOnMainThread(condition, "The condition did not become true.", timeout);
			return true;
		}
		catch (Xunit.Sdk.XunitException)
		{
			return false;
		}
	}

	static bool IsPortrait(Android.App.Activity activity) => activity.Window?.DecorView is Android.Views.View decor && decor.Height > decor.Width;

	// Landscape as the configuration says and as the decor view was laid out
	static bool IsLandscape(Android.App.Activity activity) =>
		activity.Resources?.Configuration?.Orientation is Android.Content.Res.Orientation.Landscape
		&& activity.Window?.DecorView is Android.Views.View decor
		&& decor.Width > decor.Height;

	static Android.Views.SurfaceOrientation DisplayRotation(Android.App.Activity activity)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(30))
		{
			throw new PlatformNotSupportedException("Context.Display needs API 30.");
		}

		return activity.Display?.Rotation ?? throw new InvalidOperationException("The activity has no display.");
	}

	// Asserts that the window shows its own navigation bar again, without NavigationBar's color: no overlay, and below API 35 the window's own
	// navigation bar color (ARGB).
	Task AssertOwnNavigationBarAsync(int windowNavigationBarColor, TestDialogFragment? dialog = null) =>
		InvokeOnMainThreadAsync(() =>
		{
			var window = dialog?.DialogWindow ?? AndroidSystemBars.ActivityWindow;
			Assert.Empty(AndroidSystemBars.FindTagged(window, AndroidSystemBars.NavigationBarOverlayTag));
			if (!OperatingSystem.IsAndroidVersionAtLeast(35))
			{
				Assert.Equal(windowNavigationBarColor, window.NavigationBarColor);
			}
		});

	// Creates the page in this method only (no caller local keeps it alive), lets NavigationBar apply its color and style, and disconnects its handler.
	[MethodImpl(MethodImplOptions.NoInlining)]
	async Task<WeakReference> ApplyColorAndStyleOfANewPageAsync()
	{
		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<PageHandler>(page);
			((IElementHandler)handler).DisconnectHandler();
			page.Handler = null;
		});

		await AssertNavigationBarShowsAsync(Colors.Red);
		await AssertNavigationBarStyleAsync(NavigationBarStyle.DarkContent);
		return new WeakReference(page);
	}

	// Empties NavigationBar's private lists of the windows it changed (see the constructor), like UnsupportedAndroidVersion reaches its private
	// support flag.
	static void ForgetWindowChanges()
	{
		foreach (var fieldName in new[] { "colorChanges", "styleChanges" })
		{
			var windowChanges = typeof(NavigationBar).GetField(fieldName, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as System.Collections.IList
				?? throw new InvalidOperationException($"NavigationBar no longer keeps the windows it changed in a private static list '{fieldName}'.");
			lock (windowChanges)
			{
				windowChanges.Clear();
			}
		}
	}
}