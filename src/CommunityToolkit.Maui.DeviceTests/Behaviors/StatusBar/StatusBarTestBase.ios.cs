#if !NET11_0_OR_GREATER
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;
#endif
using CommunityToolkit.Maui.DeviceTests.Runners;
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