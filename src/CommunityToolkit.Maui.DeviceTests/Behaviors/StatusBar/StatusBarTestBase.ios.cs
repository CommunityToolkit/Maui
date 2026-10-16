using CommunityToolkit.Maui.DeviceTests.Runners;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	protected partial Task ResetNativeStateAsync() => InvokeOnMainThreadAsync(IosStatusBarViews.Reset);

	/// <summary>Asserts that every window that has a status bar shows exactly one status bar view with <paramref name="color"/>.</summary>
	protected Task AssertStatusBarShowsAsync(Color color) => InvokeOnMainThreadAsync(() => AssertStatusBarShows(color));

	/// <summary>
	/// <see cref="AssertStatusBarShowsAsync"/> for code that already runs on the UI thread and must not yield (InvokeOnMainThreadAsync always
	/// posts). Each window is checked on its own: UIApplication.Windows can list more windows than the runner's (see IosStatusBarViews.Windows).
	/// </summary>
	protected static void AssertStatusBarShows(Color color)
	{
		var windows = IosStatusBarViews.WindowsWithStatusBar();
		Assert.NotEmpty(windows);
		foreach (var window in windows)
		{
			IosStatusBarViews.AssertColor(color, Assert.Single(IosStatusBarViews.TaggedSubviews(window)).BackgroundColor);
		}
	}

	/// <summary>Asserts that nothing changed the status bar since <see cref="ResetNativeStateAsync"/>: no window has a status bar view.</summary>
	protected Task AssertStatusBarUntouchedAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			foreach (var window in IosStatusBarViews.Windows)
			{
				Assert.Empty(IosStatusBarViews.TaggedSubviews(window));
			}
		});

	/// <summary>
	/// Asserts that no status bar view has a color: <see cref="AssertStatusBarUntouchedAsync"/> is too strict where the behavior's
	/// page-size handler may already have added a status bar view (StatusBar.SetBarSize adds one, without a color).
	/// </summary>
	protected Task AssertStatusBarColorNotAppliedAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			foreach (var view in IosStatusBarViews.AllTaggedSubviews())
			{
				Assert.Null(view.BackgroundColor);
			}
		});

	/// <summary>
	/// Presents a view controller over the runner's root view controller, runs <paramref name="statusBarCall"/> and asserts that it called
	/// SetNeedsStatusBarAppearanceUpdate on that topmost view controller (the one that controls the status bar). The call is counted on the
	/// view controller itself, so the check does not depend on UIKit re-reading the style, which it only does with view controller-based
	/// status bar appearance (an app may set UIViewControllerBasedStatusBarAppearance to false). With <paramref name="keepPresentingViewLoaded"/>
	/// the view controller is presented over the full screen without removing the presenting views from the window (a hosted page stays loaded).
	/// </summary>
	protected async Task AssertUpdatesTheStatusBarAppearanceAsync(Action statusBarCall, bool keepPresentingViewLoaded = false)
	{
		var rootViewController = await InvokeOnMainThreadAsync(static () => TestWindow.PlatformWindow.RootViewController ?? throw new InvalidOperationException("The runner's window has no root view controller."));
		var presented = new QueryCountingViewController(keepPresentingViewLoaded ? UIModalPresentationStyle.OverFullScreen : UIModalPresentationStyle.FullScreen);

		try
		{
			await InvokeOnMainThreadAsync(() => rootViewController.PresentViewControllerAsync(presented, false));
			await AssertEventuallyOnMainThread(() => ReferenceEquals(TopmostPresentedViewController(rootViewController), presented), "The view controller was not presented.");

			// Counted in one UI message: the toolkit's calls are synchronous, and UIKit cannot add a request of its own in between.
			var (before, after) = await InvokeOnMainThreadAsync(() =>
			{
				var requestsBefore = presented.AppearanceUpdateRequests;
				statusBarCall();
				return (requestsBefore, presented.AppearanceUpdateRequests);
			});

			Assert.True(after > before, "The topmost presented view controller did not receive SetNeedsStatusBarAppearanceUpdate.");
		}
		finally
		{
			await InvokeOnMainThreadAsync(() => presented.DismissViewControllerAsync(false));
		}
	}

	protected static UIViewController TopmostPresentedViewController(UIViewController viewController)
	{
		while (viewController.PresentedViewController is UIViewController presented)
		{
			viewController = presented;
		}

		return viewController;
	}

	/// <summary>
	/// A view controller that counts the SetNeedsStatusBarAppearanceUpdate calls it receives, and how often UIKit asks it for its status bar
	/// style (UIKit does that after SetNeedsStatusBarAppearanceUpdate when the app uses view controller-based status bar appearance).
	/// </summary>
	protected sealed class QueryCountingViewController : UIViewController
	{
		public QueryCountingViewController(UIModalPresentationStyle presentationStyle = UIModalPresentationStyle.FullScreen)
		{
			ModalPresentationStyle = presentationStyle;
			ModalPresentationCapturesStatusBarAppearance = true;
		}

		public int StyleQueries { get; private set; }

		/// <summary>How often SetNeedsStatusBarAppearanceUpdate was called on this very view controller (not on another one).</summary>
		public int AppearanceUpdateRequests { get; private set; }

		public override UIStatusBarStyle PreferredStatusBarStyle()
		{
			StyleQueries++;
			return UIStatusBarStyle.Default;
		}

		public override void SetNeedsStatusBarAppearanceUpdate()
		{
			AppearanceUpdateRequests++;
			base.SetNeedsStatusBarAppearanceUpdate();
		}
	}
}