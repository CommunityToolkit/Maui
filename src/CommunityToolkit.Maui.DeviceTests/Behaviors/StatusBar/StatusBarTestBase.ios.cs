using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	// A rotation that an earlier test left unfinished hides the status bar (iPhone in landscape) for the tests that follow.
	protected async partial Task ResetNativeStateAsync()
	{
		await InvokeOnMainThreadAsync(static () =>
		{
			IosStatusBarViews.Reset();
		});

		if (!await InvokeOnMainThreadAsync(static () => IosStatusBarViews.Snapshot(IosStatusBarViews.RunnerScene, IosStatusBarViews.RunnerWindow).IsPortraitWithStatusBar))
		{
			await RotateAsync(UIInterfaceOrientationMask.Portrait);
			var snapshot = await InvokeOnMainThreadAsync(static () => IosStatusBarViews.Snapshot(IosStatusBarViews.RunnerScene, IosStatusBarViews.RunnerWindow));
			Assert.True(snapshot.IsPortraitWithStatusBar, $"The runner is not in portrait with a status bar after the reset: {snapshot}.");
		}
	}

	protected Task AssertStatusBarShowsAsync(Color color) => InvokeOnMainThreadAsync(() => AssertStatusBarShows(color));

	// For code that already runs on the UI thread and must not yield: InvokeOnMainThreadAsync always posts.
	protected static void AssertStatusBarShows(Color color)
	{
		var windows = IosStatusBarViews.WindowsWithStatusBar();
		Assert.NotEmpty(windows);
		foreach (var window in windows)
		{
			IosStatusBarViews.AssertColor(color, Assert.Single(IosStatusBarViews.TaggedSubviews(window)).BackgroundColor);
		}
	}

	protected Task AssertStatusBarUntouchedAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			foreach (var window in IosStatusBarViews.Windows)
			{
				Assert.Empty(IosStatusBarViews.TaggedSubviews(window));
			}
		});

	// AssertStatusBarUntouchedAsync is too strict where the behavior's page-size handler may already have added a status bar view
	// (StatusBar.SetBarSize adds one, without a color).
	protected Task AssertStatusBarColorNotAppliedAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			foreach (var view in IosStatusBarViews.AllTaggedSubviews())
			{
				Assert.Null(view.BackgroundColor);
			}
		});

	// The runner's Info.plist supports portrait and both landscape orientations on iPhone.
	// Only a refusal of the app's own scene (UISceneErrorDomain) is final: an error of the system's windowing service (BSActionErrorDomain)
	// can arrive while the scene still rotates, so the test waits for the scene and fails with the error only if it never gets there.
	protected async Task RotateAsync(UIInterfaceOrientationMask orientation)
	{
		const int timeoutMilliseconds = 10000, pollMilliseconds = 50, settledMilliseconds = 400;
		var sceneRefusal = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		string? lastSystemError = null;

		await InvokeOnMainThreadAsync(() =>
		{
			WindowSceneRotation.Request(IosStatusBarViews.RunnerScene, orientation, error =>
			{
				var description = FormattableString.Invariant($"{error.Domain} error {error.Code}: {error.LocalizedDescription}");
				if (error.Domain == "UISceneErrorDomain")
				{
					sceneRefusal.TrySetResult(description);
					return;
				}

				lastSystemError = description;
				Console.WriteLine($"UIKit reported an error for the rotation to {orientation}, waiting for the scene to rotate anyway: {description}");
			});
		});

		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		var settledSince = TimeSpan.Zero;
		OrientationSnapshot? previous = null;
		OrientationSnapshot current = default;

		while (stopwatch.ElapsedMilliseconds < timeoutMilliseconds)
		{
			if (sceneRefusal.Task.IsCompleted)
			{
				throw new Xunit.Sdk.XunitException($"UIKit refused to rotate the interface to {orientation}: {sceneRefusal.Task.Result}");
			}

			current = await InvokeOnMainThreadAsync(static () => IosStatusBarViews.Snapshot(IosStatusBarViews.RunnerScene, IosStatusBarViews.RunnerWindow));
			if (!current.Matches(orientation))
			{
				previous = null;
			}
			else if (previous != current)
			{
				previous = current;
				settledSince = stopwatch.Elapsed;
			}
			else if ((stopwatch.Elapsed - settledSince).TotalMilliseconds >= settledMilliseconds)
			{
				return;
			}

			await Task.Delay(pollMilliseconds);
		}

		throw new Xunit.Sdk.XunitException($"The runner's window did not settle in {orientation} within {timeoutMilliseconds} ms: {current}. Last UIKit error: {lastSystemError ?? "none"}.");
	}

	// The window resizes the views when it rotates, and UIKit lays them out afterwards.
	protected async Task AssertStatusBarViewsCoverTheStatusBarAsync(bool isUsingSafeArea)
	{
		try
		{
			await AssertEventuallyOnMainThread(() => IosStatusBarViews.WindowsWithStatusBar().All(window => IosStatusBarViews.TaggedSubviews(window) is [var view] && view.Frame == ExpectedStatusBarViewFrame(window, isUsingSafeArea)), "The status bar views do not cover the status bar.");
		}
		catch (Xunit.Sdk.XunitException)
		{
			// The assertions below show the frames
		}

		await InvokeOnMainThreadAsync(() =>
		{
			var windows = IosStatusBarViews.WindowsWithStatusBar();
			Assert.NotEmpty(windows);
			foreach (var window in windows)
			{
				var expected = ExpectedStatusBarViewFrame(window, isUsingSafeArea).ToString() ?? string.Empty;
				var view = Assert.Single(IosStatusBarViews.TaggedSubviews(window));
				Assert.True(expected == view.Frame.ToString(), DescribeWindow(window, view, expected));
			}
		});
	}

	// Names the failing window: the runner lists system windows (UITextEffectsWindow) next to its own, and all of them get a status bar view.
	static string DescribeWindow(UIWindow window, UIView view, string expectedFrame) => FormattableString.Invariant(
		$"Expected the status bar view of {window.GetType().Name} (Hidden: {window.Hidden}, Bounds: {window.Bounds}, SafeAreaInsets: {window.SafeAreaInsets}, scene status bar frame: {IosStatusBarViews.SceneStatusBarFrame(window)}) to be {expectedFrame}, but its frame is {view.Frame}.");

	// A hidden status bar, e.g. of an iPhone in landscape, has an empty frame: the view then has no height and the width of the window.
	protected static CGRect ExpectedStatusBarViewFrame(UIWindow window, bool isUsingSafeArea)
	{
		var statusBarFrame = IosStatusBarViews.SceneStatusBarFrame(window) ?? throw new InvalidOperationException("The window has no status bar frame.");
		var frame = statusBarFrame.IsEmpty ? new CGRect(0, 0, window.Bounds.Width, 0) : statusBarFrame;

		return isUsingSafeArea ? new CGRect(frame.X, frame.Y, frame.Width, window.SafeAreaInsets.Top) : frame;
	}

	// The call is counted on the topmost view controller (the one that controls the status bar) itself, so the check does not depend on UIKit
	// re-reading the style, which it only does with view controller-based status bar appearance (an app may set
	// UIViewControllerBasedStatusBarAppearance to false).
	protected async Task AssertUpdatesTheStatusBarAppearanceAsync(Action statusBarCall)
	{
		var rootViewController = await InvokeOnMainThreadAsync(static () => TestWindow.PlatformWindow.RootViewController ?? throw new InvalidOperationException("The runner's window has no root view controller."));
		var presented = new QueryCountingViewController();

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

	protected sealed class QueryCountingViewController : UIViewController
	{
		public QueryCountingViewController()
		{
			ModalPresentationStyle = UIModalPresentationStyle.FullScreen;
			ModalPresentationCapturesStatusBarAppearance = true;
		}

		public int StyleQueries { get; private set; }

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