#if NET11_0_OR_GREATER
using CommunityToolkit.Maui.Core.Platform;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// .NET 11: a scene-based app takes the status bar style from the view controllers of its .NET MAUI windows (Window.StatusBarTheme). The
// headless runner's window has no .NET MAUI window (Application.Current.Windows is empty), so the style tests show their page in one.
public abstract partial class StatusBarTestBase
{
	/// <summary>What the toolkit traces when it sets the style while the app's Info.plist sets UIViewControllerBasedStatusBarAppearance to false.</summary>
	protected const string styleIgnoredByUIKitMessage = "Unable to apply the Status Bar Style because Info.plist sets UIViewControllerBasedStatusBarAppearance to false: remove the key from Info.plist";

	static readonly TimeSpan pageLoadTimeout = TimeSpan.FromSeconds(10);

	/// <summary>A red page: the status bar shows its white or black content over it, which the tests read from the screen.</summary>
	protected static ContentPage CreateStyleTestPage() => new() { Title = "Status bar style", BackgroundColor = Colors.Red };

	/// <summary>
	/// Runs <paramref name="action"/> as if the app's Info.plist set UIViewControllerBasedStatusBarAppearance to <paramref name="value"/> (as if it had
	/// no such key when <see langword="null"/>) and returns what the toolkit traced meanwhile. The runner's Info.plist has no such key, and UIKit reads
	/// the real one at launch: only what the toolkit reads is replaced.
	/// </summary>
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

	/// <summary>
	/// Shows <paramref name="page"/> in a .NET MAUI window the way an app does and runs <paramref name="action"/> on the UI thread once the page is
	/// loaded: a test application (Application.Current until the end) creates the window, the window's own handler shows it in a new full-screen
	/// UIWindow over the runner's window, and the window is created and activated. <paramref name="kind"/> puts the page into a NavigationPage,
	/// Shell, TabbedPage or FlyoutPage, or presents it modally over another page; their bars are red like the page. Afterwards the window is
	/// deactivated and destroyed, and the runner's window is the key window of the scene again.
	/// </summary>
	protected Task RunInMauiWindowAsync(StatusBarWindowPage kind, ContentPage page, Func<HostedMauiWindow, Task> action) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var previousApplication = Application.Current;
			var root = CreateRootPage(kind, page);
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

				if (kind is StatusBarWindowPage.ModalPage)
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
				// out of the application
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
				// NavigationPage (MAUI 11 RC1) answers ChildViewControllerForStatusBarHidden with itself, which UIKit follows until the stack overflows
				root.DisconnectHandlers();
				page.DisconnectHandlers();
			}
		});

	/// <summary>
	/// Asserts the status bar style that the window shows: the view controller that UIKit asks returns <paramref name="expected"/>, UIKit applies
	/// it to the status bar of the scene, and with LightContent or DarkContent the status bar content on the screen is white or black. Default
	/// is not read from the screen: UIKit chooses its color itself (on iOS 27 the content kept its previous color over the same page).
	/// </summary>
	protected static async Task AssertStatusBarStyleAsync(HostedMauiWindow mauiWindow, UIStatusBarStyle expected)
	{
		var provider = IosStatusBarStyle.StyleProvider(mauiWindow.PlatformWindow);
		var preferred = provider.PreferredStatusBarStyle();
		Assert.True(preferred == expected, $"{provider.GetType().Name}, which UIKit asks for the status bar style of the window, returned {IosStatusBarStyle.Name(preferred)} instead of {IosStatusBarStyle.Name(expected)}.");

		var statusBarManager = mauiWindow.Scene.StatusBarManager ?? throw new InvalidOperationException("The scene has no status bar.");
		var isApplied = await AssertHelpers.Wait(() => statusBarManager.StatusBarStyle == expected, timeout: 3000, interval: 20);
		Assert.True(isApplied, $"UIKit shows {IosStatusBarStyle.Name(statusBarManager.StatusBarStyle)} in the status bar of the scene instead of {IosStatusBarStyle.Name(expected)}.");

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

	/// <summary>A .NET MAUI window that <see cref="RunInMauiWindowAsync"/> shows, the application that created it, and its UIWindow.</summary>
	protected sealed record HostedMauiWindow(MauiWindowTestApplication Application, Window Window, UIWindow PlatformWindow)
	{
		public UIWindowScene Scene => PlatformWindow.WindowScene ?? throw new InvalidOperationException("The window left its scene.");
	}

	/// <summary>The application of the windows that the tests show (Application.Current while they run).</summary>
	protected sealed class MauiWindowTestApplication : Application
	{
		Page? pageOfTheNextWindow;

		/// <summary>Creates a window of the application for <paramref name="page"/> the way .NET MAUI does for a new scene (IApplication.CreateWindow).</summary>
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

	// The MauiContext of the shown window: the test's services and handlers, and the window's own UIWindow (WindowHandler takes its platform view
	// from it, as from the window scope of an app).
	sealed class PlatformWindowMauiContext(IMauiContext context, UIWindow platformWindow) : IMauiContext, IServiceProvider
	{
		public IServiceProvider Services => this;

		public IMauiHandlersFactory Handlers => context.Handlers;

		public object? GetService(Type serviceType) => serviceType == typeof(UIWindow) ? platformWindow : context.Services.GetService(serviceType);
	}
}
#endif