using Android.App;
using Android.Graphics.Drawables;
using Android.Views;
using AndroidX.Core.View;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class UseMauiCommunityToolkitTests
{
	// PageHandler.Mapper is process-wide and the runner's own UseMauiCommunityToolkit call has already mapped the properties, so the test
	// first replaces the mapping with a sentinel. Building the test's MauiApp (SetupBuilder) runs UseMauiCommunityToolkit again: the
	// mapping that is in place afterwards must be the toolkit's, which sets the light/dark appearance of the real navigation bar.
	[Fact]
	public async Task UseMauiCommunityToolkitRemapsTheNavigationBarStyleOntoThePageHandler()
	{
		var styleKey = NavigationBar.StyleProperty.PropertyName;
		var sentinelCalls = 0;
		PageHandler.Mapper.Add(styleKey, (_, _) => sentinelCalls++);
		try
		{
			SetupBuilder();

			await InvokeOnMainThreadAsync(() =>
			{
				var (runnerWindow, runnerWindowBefore) = ReadRunnerWindow();
				try
				{
					var page = new ContentPage();
					NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);
					var handler = CreateHandler<PageHandler>(page);
					var window = Assert.IsType<Activity>(handler.PlatformView.Context, exactMatch: false).Window!;
					var insetsController = WindowCompat.GetInsetsController(window, window.DecorView)!;
					insetsController.AppearanceLightNavigationBars = false;

					PageHandler.Mapper.UpdateProperty(handler, page, styleKey);

					Assert.Equal(0, sentinelCalls);
					Assert.True(insetsController.AppearanceLightNavigationBars, "The toolkit's mapping did not set the dark navigation bar content.");
				}
				finally
				{
					AndroidSystemBars.Restore(runnerWindow, runnerWindowBefore);
				}
			});
		}
		finally
		{
			PageHandler.Mapper.Add(styleKey, NavigationBar.MapNavigationStyleProperty);
		}
	}

	[Fact]
	public async Task UseMauiCommunityToolkitRemapsTheNavigationBarColorOntoThePageHandler()
	{
		const string navigationBarOverlayTag = "NavigationBarOverlay";
		var colorKey = NavigationBar.ColorProperty.PropertyName;
		var sentinelCalls = 0;
		PageHandler.Mapper.Add(colorKey, (_, _) => sentinelCalls++);
		try
		{
			SetupBuilder();

			await InvokeOnMainThreadAsync(() =>
			{
				var (runnerWindow, runnerWindowBefore) = ReadRunnerWindow();
				try
				{
					var page = new ContentPage();
					NavigationBar.SetColor(page, Colors.Red);
					var handler = CreateHandler<PageHandler>(page);
					var window = Assert.IsType<Activity>(handler.PlatformView.Context, exactMatch: false).Window!;
					var decorGroup = (ViewGroup)window.DecorView;

					PageHandler.Mapper.UpdateProperty(handler, page, colorKey);

					Assert.Equal(0, sentinelCalls);
					if (OperatingSystem.IsAndroidVersionAtLeast(35))
					{
						var overlay = decorGroup.FindViewWithTag(navigationBarOverlayTag);
						Assert.NotNull(overlay);
						Assert.Equal(Colors.Red.ToPlatform().ToArgb(), Assert.IsType<ColorDrawable>(overlay.Background).Color.ToArgb());
					}
					else
					{
#pragma warning disable CA1422 // Validate platform compatibility: Window.NavigationBarColor is obsolete from Android 15, the toolkit uses it below that
						Assert.Equal(Colors.Red.ToPlatform().ToArgb(), window.NavigationBarColor);
#pragma warning restore CA1422
					}
				}
				finally
				{
					// Removes the overlay (API 35+), and puts back the navigation bar color, the decor's fitting and the window flags.
					AndroidSystemBars.Restore(runnerWindow, runnerWindowBefore);
				}
			});
		}
		finally
		{
			PageHandler.Mapper.Add(colorKey, NavigationBar.MapNavigationColorProperty);
		}
	}

	// Creating the page handler already runs the toolkit's mapping, which changes the runner's activity window (navigation bar appearance or
	// color, the decor's fitting from API 35, and the TranslucentStatus/DrawsSystemBarBackgrounds flags through GetCurrentWindow). Read the
	// window's state before the handler exists, so that the test can put all of it back: the status bar tests capture that window's state
	// once per app run (AndroidSystemBars.CaptureOriginalState), and a change left behind here became their "original" state.
	(Android.Views.Window Window, AndroidSystemBars.WindowState State) ReadRunnerWindow()
	{
		var window = Assert.IsType<Activity>(MauiContext.Context, exactMatch: false).Window ?? throw new InvalidOperationException("The runner's activity has no window.");
		return (window, AndroidSystemBars.WindowState.Read(window));
	}
}