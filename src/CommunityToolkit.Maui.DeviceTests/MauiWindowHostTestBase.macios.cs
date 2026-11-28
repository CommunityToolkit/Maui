using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// The headless runner's window has no .NET MAUI window (Application.Current.Windows is empty), so a test that needs real .NET MAUI navigation
// (modal pages, Shell routes, popups) shows its root page in a window of its own.
public abstract partial class MauiWindowHostTestBase
{
	static readonly TimeSpan pageLoadTimeout = TimeSpan.FromSeconds(10);

	// The scene of the runner is the only connected scene; the windows of the tests are created in it
	static UIWindowScene RunnerScene =>
		UIApplication.SharedApplication.ConnectedScenes.OfType<UIWindowScene>().FirstOrDefault()
		?? throw new InvalidOperationException("The runner has no connected window scene.");

	// page is root or one of its pages, or with isPagePresentedModally the page that the window presents modally over root.
	protected Task RunInMauiWindowAsync(Page root, bool isPagePresentedModally, ContentPage page, Func<HostedMauiWindow, Task> action) =>
		InvokeOnMainThreadAsync(async () =>
		{
			var previousApplication = Application.Current;
			var scene = RunnerScene;
			var platformWindow = new UIWindow(scene);
#if MACCATALYST
			var runnerGeometryBefore = RunnerWindowGeometry.Capture();
#endif
			Window? window = null;
			bool isCreated = false, isActivated = false;

			try
			{
				var application = new MauiWindowTestApplication();
				window = application.CreateWindowFor(root);
				var windowContext = new PlatformWindowMauiContext(MauiContext, platformWindow);
#if MACCATALYST
				// The window of the test is a guest in the scene of the runner. WindowHandler would send its frame (iPad points) as the scene's
				// frame (Mac points), which moves and widens the runner's window, and it would lift the scene's size limits.
				window.Handler = new GuestWindowHandler(windowContext);
#else
				platformWindow.SetWindowHandler(window, windowContext);
#endif
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

#if MACCATALYST
			var runnerGeometryAfter = RunnerWindowGeometry.Capture();
			Assert.True(runnerGeometryBefore == runnerGeometryAfter,
				$"Hosting a window changed the window of the runner from {runnerGeometryBefore} to {runnerGeometryAfter}. AttachAndRun lays out later tests in the runner's window.");
#endif
		});

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