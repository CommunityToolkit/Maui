using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// An app whose Info.plist keeps view controller-based status bar appearance, such as the runner, takes the status bar style from the view
// controllers of its windows: on .NET 11 those of a .NET MAUI window return the style of its Window.StatusBarTheme, and on .NET 10 every view
// controller that does not choose a style returns the one that StatusBar.SetStyle requested. The headless runner's window has no .NET MAUI window
// (Application.Current.Windows is empty), so the style tests show their page in one.
public abstract partial class StatusBarTestBase
{
	protected const string styleIgnoredByUIKitMessage = "Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist";

	static readonly TimeSpan pageLoadTimeout = TimeSpan.FromSeconds(10);

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

	/// <summary>
	/// <see cref="RunInMauiWindowAsync(StatusBarWindowPage, ContentPage, Func{HostedMauiWindow, Task})"/> with a root page that the test builds itself:
	/// <paramref name="page"/> is <paramref name="root"/> or one of its pages, or with <paramref name="isPagePresentedModally"/> the page that the window
	/// presents modally over <paramref name="root"/>.
	/// </summary>
	protected Task RunInMauiWindowAsync(Page root, bool isPagePresentedModally, ContentPage page, Func<HostedMauiWindow, Task> action) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var previousApplication = Application.Current;
			var scene = IosStatusBarViews.RunnerScene;
			var platformWindow = new UIWindow(scene) { Frame = scene.CoordinateSpace.Bounds };
			Window? window = null;
			bool isCreated = false, isActivated = false;

			try
			{
				var application = new MauiWindowTestApplication();
				window = application.CreateWindowFor(root);
				platformWindow.SetWindowHandler(window, new PlatformWindowMauiContext(MauiContext, platformWindow));
				((IWindow)window).Created();
				isCreated = true;
				platformWindow.MakeKeyAndVisible();
				((IWindow)window).Activated(); // .NET MAUI presents modal pages only in an activated window
				isActivated = true;

				if (isPagePresentedModally)
				{
					await root.OnLoadedAsync(pageLoadTimeout);
					await window.Navigation.PushModalAsync(page, false);
				}

				await page.OnLoadedAsync(pageLoadTimeout);

				await action(new HostedMauiWindow(application, window, platformWindow));
			}
			finally
			{
				if (platformWindow.RootViewController?.PresentedViewController is not null)
				{
					await platformWindow.RootViewController.DismissViewControllerAsync(false);
				}

				// What .NET MAUI's scene delegate does when the scene disconnects: Destroying disconnects the window's handler and takes the window
				// out of the application.
				if (window is not null && isActivated)
				{
					((IWindow)window).Deactivated();
				}

				if (window is not null && isCreated)
				{
					((IWindow)window).Destroying();
				}

				platformWindow.Hidden = true;
				platformWindow.RootViewController = null;
				platformWindow.WindowScene = null;
				platformWindow.Dispose();
				TestWindow.PlatformWindow.MakeKeyAndVisible();
				Application.Current = previousApplication;

				// Only once UIKit no longer asks the view controllers of the pages: with the page's handler disconnected, the view controller of a
				// NavigationPage (MAUI 11 RC1) answers ChildViewControllerForStatusBarHidden with itself, which UIKit follows until the stack overflows.
				root.DisconnectHandlers();
				page.DisconnectHandlers();
			}
		});

	protected static async Task AssertStatusBarStyleAsync(HostedMauiWindow mauiWindow, UIStatusBarStyle expected)
	{
		var provider = IosStatusBarStyle.StyleProvider(mauiWindow.PlatformWindow);
		var preferred = provider.PreferredStatusBarStyle();
		Assert.True(preferred == expected, $"{provider.GetType().Name}, which UIKit asks for the status bar style of the window, returned {IosStatusBarStyle.Name(preferred)} instead of {IosStatusBarStyle.Name(expected)}.");

		var statusBarManager = mauiWindow.Scene.StatusBarManager ?? throw new InvalidOperationException("The scene has no status bar.");
		var isApplied = await AssertHelpers.Wait(() => statusBarManager.StatusBarStyle == expected, timeout: 3000, interval: 20);
		Assert.True(isApplied, $"UIKit shows {IosStatusBarStyle.Name(statusBarManager.StatusBarStyle)} in the status bar of the scene instead of {IosStatusBarStyle.Name(expected)}.");

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

	protected sealed record HostedMauiWindow(MauiWindowTestApplication Application, Window Window, UIWindow PlatformWindow)
	{
		public UIWindowScene Scene => PlatformWindow.WindowScene ?? throw new InvalidOperationException("The window left its scene.");
	}

	protected sealed class MauiWindowTestApplication : Application
	{
		Page? pageOfTheNextWindow;

		// Through IApplication.CreateWindow, the way .NET MAUI creates the window of a new scene.
		public Window CreateWindowFor(Page page)
		{
			pageOfTheNextWindow = page;
			try
			{
				return (Window)((IApplication)this).CreateWindow(null);
			}
			finally
			{
				pageOfTheNextWindow = null;
			}
		}

		protected override Window CreateWindow(IActivationState? activationState) =>
			new(pageOfTheNextWindow ?? throw new InvalidOperationException($"Create the windows of the test application with {nameof(CreateWindowFor)}."));
	}

	// WindowHandler takes its platform view from the MauiContext, as from the window scope of an app, so this context adds the window's own
	// UIWindow to the test's services and handlers.
	sealed class PlatformWindowMauiContext(IMauiContext context, UIWindow platformWindow) : IMauiContext, IServiceProvider
	{
		public IServiceProvider Services => this;

		public IMauiHandlersFactory Handlers => context.Handlers;

		public object? GetService(Type serviceType) => serviceType == typeof(UIWindow) ? platformWindow : context.Services.GetService(serviceType);
	}
}