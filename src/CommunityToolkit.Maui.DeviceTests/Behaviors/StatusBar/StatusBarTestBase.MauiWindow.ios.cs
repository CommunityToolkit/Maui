using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// An app whose Info.plist keeps view controller-based status bar appearance, such as the runner, takes the status bar style from the view controllers
// of its windows, and those of a .NET MAUI window return the style of its Window.StatusBarTheme. The headless runner's window has no .NET MAUI window
// (Application.Current.Windows is empty), so the style tests show their page in one.
public abstract partial class StatusBarTestBase
{
	protected const string styleIgnoredByUIKitMessage = "Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist";

	// Red because the tests read the status bar's white or black content from the screen, and the background must be neither.
	protected static ContentPage CreateStyleTestPage() => new() { Title = "Status bar style", BackgroundColor = Colors.Red };

	// The runner's Info.plist has no UIViewControllerBasedStatusBarAppearance key, and UIKit reads the real one at launch, so only the value that
	// the toolkit reads is replaced.
	private protected static CapturingTraceListener CaptureTraceWithViewControllerBasedStatusBarAppearance(bool? value, Action action)
	{
		var readViewControllerBasedStatusBarAppearance = StatusBar.ReadViewControllerBasedStatusBarAppearance;
		using var infoPlistValue = value is bool isViewControllerBased ? NSNumber.FromBoolean(isViewControllerBased) : null;
		StatusBar.ReadViewControllerBasedStatusBarAppearance = () => infoPlistValue;

		try
		{
			return CapturingTraceListener.Capture(action);
		}
		finally
		{
			StatusBar.ReadViewControllerBasedStatusBarAppearance = readViewControllerBasedStatusBarAppearance;
		}
	}

	// The styles of the whole app are recorded instead of passed to UIKit: the runner's Info.plist keeps view controller-based status bar appearance, so
	// UIKit would ignore them. isAppliedByUIKit is what UIKit reports back: true in an app built with an iOS SDK before 27.0 whose Info.plist sets the
	// key to false, false in an app built with a later SDK.
	private protected static (CapturingTraceListener Listener, List<UIStatusBarStyle> StylesOfTheWholeApp) CaptureStylesOfTheWholeApp(bool? viewControllerBasedStatusBarAppearance, bool isAppliedByUIKit, Action action)
	{
		var trySetApplicationStatusBarStyle = StatusBar.TrySetApplicationStatusBarStyle;
		var stylesOfTheWholeApp = new List<UIStatusBarStyle>();
		StatusBar.TrySetApplicationStatusBarStyle = style =>
		{
			stylesOfTheWholeApp.Add(style);
			return isAppliedByUIKit;
		};

		try
		{
			return (CaptureTraceWithViewControllerBasedStatusBarAppearance(viewControllerBasedStatusBarAppearance, action), stylesOfTheWholeApp);
		}
		finally
		{
			StatusBar.TrySetApplicationStatusBarStyle = trySetApplicationStatusBarStyle;
		}
	}

	protected Task RunInMauiWindowAsync(StatusBarWindowPage kind, ContentPage page, Func<HostedMauiWindow, Task> action) =>
		RunInMauiWindowAsync(CreateRootPage(kind, page), kind is StatusBarWindowPage.ModalPage, page, action);

	// Default gives the view controller its own style back (IosStatusBarStyle.UIKitStyle), which on iOS 26 is not Default for a UINavigationController.
	protected static async Task AssertStatusBarStyleAsync(HostedMauiWindow mauiWindow, UIStatusBarStyle expected)
	{
		var provider = IosStatusBarStyle.StyleProvider(mauiWindow.PlatformWindow);
		var expectedStyle = expected is UIStatusBarStyle.Default ? IosStatusBarStyle.UIKitStyle(provider) : expected;
		var preferred = provider.PreferredStatusBarStyle();
		Assert.True(preferred == expectedStyle, $"{provider.GetType().Name}, which UIKit asks for the status bar style of the window, returned {IosStatusBarStyle.Name(preferred)} instead of {IosStatusBarStyle.Name(expectedStyle)}{(expected is UIStatusBarStyle.Default ? ", the style of UIKit's own implementation" : string.Empty)}.");

		var statusBarManager = mauiWindow.Scene.StatusBarManager ?? throw new InvalidOperationException("The scene has no status bar.");
		var isApplied = await AssertHelpers.Wait(() => statusBarManager.StatusBarStyle == expectedStyle, timeout: 3000, interval: 20);
		Assert.True(isApplied, $"UIKit shows {IosStatusBarStyle.Name(statusBarManager.StatusBarStyle)} in the status bar of the scene instead of {IosStatusBarStyle.Name(expectedStyle)}.");

		// Default is not read from the screen: UIKit chooses its color itself (on iOS 27 the content kept its previous color over the same page).
		if (expected is UIStatusBarStyle.LightContent or UIStatusBarStyle.DarkContent)
		{
			var isLight = expected is UIStatusBarStyle.LightContent;
			var content = default(StatusBarContent);
			var isShown = await AssertHelpers.Wait(() =>
			{
				content = IosStatusBarStyle.CaptureContent(mauiWindow.Scene);
				return isLight ? content.IsLight : content.IsDark;
			}, timeout: 3000, interval: 50);
			Assert.True(isShown, $"The status bar on the screen does not show the {(isLight ? "white" : "black")} content of {IosStatusBarStyle.Name(expected)}: it has {content}.");
		}
	}

	static Page CreateRootPage(StatusBarWindowPage kind, ContentPage page) => kind switch
	{
		StatusBarWindowPage.ContentPage => page,
		StatusBarWindowPage.NavigationPage => new NavigationPage(page) { BarBackgroundColor = Colors.Red },
		StatusBarWindowPage.Shell => new Shell { BackgroundColor = Colors.Red, Items = { new ShellContent { Content = page } } },
		StatusBarWindowPage.TabbedPage => new TabbedPage { Children = { page, CreateStyleTestPage() } },
		StatusBarWindowPage.FlyoutPage => new FlyoutPage { Flyout = CreateStyleTestPage(), Detail = page },
		StatusBarWindowPage.ModalPage => CreateStyleTestPage(),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown page kind.")
	};
}