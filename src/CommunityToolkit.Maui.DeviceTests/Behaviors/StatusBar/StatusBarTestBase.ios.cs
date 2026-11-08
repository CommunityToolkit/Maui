#if !NET11_0_OR_GREATER
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
#endif
using CommunityToolkit.Maui.DeviceTests.Runners;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

public abstract partial class StatusBarTestBase
{
	// .NET 10: a style that a test set stays the style of the whole app until SetStyle(Default), also after the test ends
	protected partial Task ResetNativeStateAsync() =>
		InvokeOnMainThreadAsync(static () =>
		{
			IosStatusBarViews.Reset();
#if !NET11_0_OR_GREATER
			StatusBar.SetStyle(StatusBarStyle.Default);
#endif
		});

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
	protected async Task RotateAsync(UIInterfaceOrientationMask orientation)
	{
		Skip.IfNot(OperatingSystem.IsIOSVersionAtLeast(16), "Rotating the interface needs UIWindowScene.RequestGeometryUpdate (iOS 16); the simulators of the pool run iOS 27.");

		var isLandscape = orientation is UIInterfaceOrientationMask.Landscape or UIInterfaceOrientationMask.LandscapeLeft or UIInterfaceOrientationMask.LandscapeRight;
		var refusal = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
		await InvokeOnMainThreadAsync(() =>
		{
			if (OperatingSystem.IsIOSVersionAtLeast(16))
			{
				IosStatusBarViews.RunnerScene.RequestGeometryUpdate(new UIWindowSceneGeometryPreferencesIOS(orientation), error => refusal.TrySetResult(error.LocalizedDescription));
			}
		});

		await AssertEventuallyOnMainThread(() => refusal.Task.IsCompleted || IsLandscape(TestWindow.PlatformWindow) == isLandscape, $"The runner's window did not rotate to {orientation}.");
		Assert.False(refusal.Task.IsCompleted, $"UIKit refused to rotate the interface to {orientation}: {(refusal.Task.IsCompleted ? refusal.Task.Result : string.Empty)}");
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
				Assert.Equal(ExpectedStatusBarViewFrame(window, isUsingSafeArea).ToString(), Assert.Single(IosStatusBarViews.TaggedSubviews(window)).Frame.ToString());
			}
		});
	}

	// A hidden status bar, e.g. of an iPhone in landscape, has an empty frame: the view then has no height and the width of the window.
	protected static CGRect ExpectedStatusBarViewFrame(UIWindow window, bool isUsingSafeArea)
	{
		var statusBarFrame = IosStatusBarViews.SceneStatusBarFrame(window) ?? throw new InvalidOperationException("The window has no status bar frame.");
		var frame = statusBarFrame.IsEmpty ? new CGRect(0, 0, window.Bounds.Width, 0) : statusBarFrame;

		return isUsingSafeArea ? new CGRect(frame.X, frame.Y, frame.Width, window.SafeAreaInsets.Top) : frame;
	}

	static bool IsLandscape(UIWindow window) => window.Bounds.Width > window.Bounds.Height;

	// The call is counted on the topmost view controller (the one that controls the status bar) itself, so the check does not depend on UIKit
	// re-reading the style, which it only does with view controller-based status bar appearance (an app may set
	// UIViewControllerBasedStatusBarAppearance to false).
	protected async Task AssertUpdatesTheStatusBarAppearanceAsync(Action statusBarCall, bool keepPresentingViewLoaded = false)
	{
		var rootViewController = await InvokeOnMainThreadAsync(static () => TestWindow.PlatformWindow.RootViewController ?? throw new InvalidOperationException("The runner's window has no root view controller."));
		// OverFullScreen leaves the presenting views in the window, so a hosted page stays loaded.
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

	protected sealed class QueryCountingViewController : UIViewController
	{
		public QueryCountingViewController(UIModalPresentationStyle presentationStyle = UIModalPresentationStyle.FullScreen)
		{
			ModalPresentationStyle = presentationStyle;
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