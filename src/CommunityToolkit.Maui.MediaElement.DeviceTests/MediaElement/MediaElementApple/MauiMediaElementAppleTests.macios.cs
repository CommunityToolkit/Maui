using AVKit;
using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.DeviceTests;

// MauiMediaElement.macios.cs: the platform view of MediaElement. It hosts the view of the AVPlayerViewController. On iOS 16+ and Mac
// Catalyst 16.1+ it also adds the AVPlayerViewController as a child of the view controller that its responder chain reaches (otherwise
// the playback controls are not shown), whenever it moves to a superview or a window or lays out: it moves the controller to the new
// parent when the view moves, cancels out the parent view's safe area insets, re-attaches the controller when the playback controls are
// shown, and DetachFromParentViewController removes it. The UIKit parent-child relations and the additional safe area insets are what the
// tests read. The branch for iOS / Mac Catalyst before 16 (no parent view controller at all) cannot run: the oldest runtime is 18.
[Category(TestCategory.MediaElementApple)]
[Collection(TestCollections.DisableParallelization)]
public class MauiMediaElementAppleTests : AppleMediaElementTestBase
{
	static readonly UIEdgeInsets parentInsets = new(10, 20, 30, 40);

	static UIViewController? TopMostViewController()
	{
		var controller = KeyWindow().RootViewController;

		while (controller?.PresentedViewController is not null)
		{
			controller = controller.PresentedViewController;
		}

		return controller;
	}

	static UIWindow KeyWindow() =>
		UIApplication.SharedApplication.ConnectedScenes
			.OfType<UIWindowScene>()
			.SelectMany(static scene => scene.Windows)
			.First(static window => window.IsKeyWindow);

	// Removes the view from its superview and the controller from its parent with UIKit's own containment calls, so a test that is not
	// about DetachFromParentViewController does not depend on it.
	static void Cleanup(MauiMediaElement? view, AVPlayerViewController controller)
	{
		view?.RemoveFromSuperview();
		controller.WillMoveToParentViewController(null);
		controller.RemoveFromParentViewController();
		view?.Dispose();
		controller.Dispose();
	}

	[Fact]
	public async Task ConstructorAddsThePlayerViewAsSubview()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var controller = new AVPlayerViewController();

			var view = new MauiMediaElement(controller, CreateMediaElement());

			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Assert.Equal(CGRect.Empty, controller.View!.Frame);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task ConstructorThrowsWhenThePlayerViewControllerHasNoView()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var controller = new AppleViewlessPlayerViewController();

			var exception = Assert.Throws<InvalidOperationException>(() => new MauiMediaElement(controller, CreateMediaElement()));

			Assert.Equal("playerViewController.View cannot be null.", exception.Message);
		});
	}

	[Fact]
	public async Task ConstructorThrowsWhenThereIsNoVirtualView()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			using var controller = new AVPlayerViewController();

			var exception = Assert.Throws<ArgumentNullException>(() => new MauiMediaElement(controller, null!));

			Assert.Equal("virtualView", exception.ParamName);
		});
	}

	[Fact]
	public async Task ConstructorDoesNotAddThePlayerViewControllerToAParentViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var controller = new AppleParentTrackingPlayerViewController();

			// A new view has no superview, so its responder chain reaches no view controller.
			var view = new MauiMediaElement(controller, CreateMediaElement());

			Assert.Null(controller.ParentViewController);
			Assert.Empty(controller.MovedToParents);
			Assert.Equal(UIEdgeInsets.Zero, controller.AdditionalSafeAreaInsets);
			Cleanup(view, controller);
		});
	}

	// The view is added to the view of a view controller directly, or through views in between (the responder chain goes up the superviews).
	[Theory]
	[InlineData(0)]
	[InlineData(2)]
	public async Task MovingToAViewOfAViewControllerAddsThePlayerViewControllerToIt(int viewsInBetween)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new UIViewController();
			var superview = parent.View!;
			for (var index = 0; index < viewsInBetween; index++)
			{
				var container = new UIView();
				superview.AddSubview(container);
				superview = container;
			}

			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			superview.AddSubview(view);

			Assert.Same(parent, controller.ParentViewController);
			Assert.Contains(controller, parent.ChildViewControllers);
			Assert.Equal(new UIViewController?[] { parent }, controller.MovedToParents);

			// The player view stays in the MauiMediaElement: only the controller is added to the parent.
			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task MovingToAViewOutsideOfAnyViewControllerDoesNotAddThePlayerViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var container = new UIView();
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			container.AddSubview(view);
			view.SetNeedsLayout();
			view.LayoutIfNeeded();

			Assert.Null(controller.ParentViewController);
			Assert.Empty(controller.MovedToParents);
			Assert.Equal(UIEdgeInsets.Zero, controller.AdditionalSafeAreaInsets);
			Cleanup(view, controller);
		});
	}

	// A view hierarchy that is built first and added to a view controller's view in the window afterwards: the MauiMediaElement keeps its
	// superview, so it is MovedToWindow that adds the controller.
	[Fact]
	public async Task MovingIntoTheWindowAddsThePlayerViewControllerToTheViewControllerOfTheNewSuperviews()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var root = TopMostViewController()!;
			var container = new UIView();
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			container.AddSubview(view);
			Assert.Null(controller.ParentViewController);

			root.View!.AddSubview(container);
			try
			{
				Assert.NotNull(view.Window);
				Assert.Same(root, controller.ParentViewController);
				Assert.Equal(new UIViewController?[] { root }, controller.MovedToParents);
			}
			finally
			{
				container.RemoveFromSuperview();
				Cleanup(view, controller);
			}
		});
	}

	[Fact]
	public async Task AttachingCancelsOutTheTopAndBottomSafeAreaInsetsOfTheParentView()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new AppleInsetViewController(parentInsets);
			var controller = new AVPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			parent.View!.AddSubview(view);

			// The top and bottom insets of the parent are cancelled out; left and right are the test below.
			Assert.Equal(-10, (double)controller.AdditionalSafeAreaInsets.Top);
			Assert.Equal(-30, (double)controller.AdditionalSafeAreaInsets.Bottom);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task AttachingCancelsOutTheLeftAndRightSafeAreaInsetsOfTheParentView()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new AppleInsetViewController(parentInsets);
			var controller = new AVPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			parent.View!.AddSubview(view);

			Assert.Equal(-20, (double)controller.AdditionalSafeAreaInsets.Left);
			Assert.Equal(-40, (double)controller.AdditionalSafeAreaInsets.Right);
			Cleanup(view, controller);
		});
	}

	// The effect of the additional insets above on the safe area that UIKit gives the player view, in a real window: a player view that
	// covers the parent's unsafe edges and one inside the parent's safe area.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AttachingDoesNotAddTheSafeAreaInsetsOfTheParentViewControllerToThePlayerView(bool playerViewIsInsideTheSafeArea)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(async () =>
		{
			var window = KeyWindow();
			var root = TopMostViewController()!;
			var parentController = new UIViewController { AdditionalSafeAreaInsets = parentInsets };
			root.AddChildViewController(parentController);
			parentController.View!.Frame = root.View!.Bounds;
			root.View.AddSubview(parentController.View);
			parentController.DidMoveToParentViewController(root);
			var controller = new AVPlayerViewController();
			MauiMediaElement? view = null;

			try
			{
				// Every edge of the parent has a safe area inset.
				await AssertionExtensions.AssertEventually(() =>
				{
					window.LayoutIfNeeded();
					var insets = parentController.View.SafeAreaInsets;
					return insets.Top >= 10 && insets.Left >= 20 && insets.Bottom >= 30 && insets.Right >= 40;
				}, timeout: 2000, interval: 20, message: "The parent view controller did not get its safe area insets.");
				var insetsOfTheParent = parentController.View.SafeAreaInsets;

				view = new MauiMediaElement(controller, CreateMediaElement());
				var bounds = parentController.View.Bounds;
				view.Frame = playerViewIsInsideTheSafeArea
					? new CGRect(
						bounds.X + insetsOfTheParent.Left + 5,
						bounds.Y + insetsOfTheParent.Top + 5,
						bounds.Width - insetsOfTheParent.Left - insetsOfTheParent.Right - 10,
						bounds.Height - insetsOfTheParent.Top - insetsOfTheParent.Bottom - 10)
					: bounds;
				parentController.View.AddSubview(view);
				window.LayoutIfNeeded();
				Assert.Same(parentController, controller.ParentViewController);

				// The player view gets the parent's safe area where the two overlap, like the view that contains it; the additional insets of
				// the AVPlayerViewController must not add the parent's insets to that.
				var containerInsets = view.SafeAreaInsets;
				var playerInsets = controller.View!.SafeAreaInsets;
				Assert.True(
					playerInsets.Top <= containerInsets.Top
						&& playerInsets.Left <= containerInsets.Left
						&& playerInsets.Bottom <= containerInsets.Bottom
						&& playerInsets.Right <= containerInsets.Right,
					$"Safe area insets of the player view: {Format(playerInsets)}; of the view that contains it: {Format(containerInsets)}; of the parent: {Format(insetsOfTheParent)}.");
			}
			finally
			{
				Cleanup(view, controller);
				parentController.WillMoveToParentViewController(null);
				parentController.View.RemoveFromSuperview();
				parentController.RemoveFromParentViewController();
			}
		});

		static string Format(UIEdgeInsets insets) => $"(top {insets.Top}, left {insets.Left}, bottom {insets.Bottom}, right {insets.Right})";
	}

	[Fact]
	public async Task LayoutAppliesTheSafeAreaInsetsOfTheParentViewWhenTheyChange()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new AppleInsetViewController(parentInsets);
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			parent.View!.AddSubview(view);

			((AppleInsetView)parent.View).Insets = new UIEdgeInsets(1, 2, 3, 4);
			view.SetNeedsLayout();
			view.LayoutIfNeeded();

			Assert.Equal(new UIEdgeInsets(-1, -2, -3, -4), controller.AdditionalSafeAreaInsets);

			// The controller stays where it is.
			Assert.Equal(new UIViewController?[] { parent }, controller.MovedToParents);
			Cleanup(view, controller);
		});
	}

	// Each edge of the parent's safe area is compared: a change of a single inset is applied as well.
	// Catches: the comparison of the current and the applied insets leaving out an edge.
	[Theory]
	[InlineData("Top")]
	[InlineData("Left")]
	[InlineData("Bottom")]
	[InlineData("Right")]
	public async Task LayoutAppliesAChangeOfASingleSafeAreaInsetOfTheParentView(string edge)
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new AppleInsetViewController(parentInsets);
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			parent.View!.AddSubview(view);
			view.SetNeedsLayout();
			view.LayoutIfNeeded();
			Assert.Equal(new UIEdgeInsets(-10, -20, -30, -40), controller.AdditionalSafeAreaInsets);

			var changed = edge switch
			{
				"Top" => new UIEdgeInsets(5, 20, 30, 40),
				"Left" => new UIEdgeInsets(10, 5, 30, 40),
				"Bottom" => new UIEdgeInsets(10, 20, 5, 40),
				"Right" => new UIEdgeInsets(10, 20, 30, 5),
				_ => throw new ArgumentOutOfRangeException(nameof(edge), edge, null)
			};
			((AppleInsetView)parent.View).Insets = changed;
			view.SetNeedsLayout();
			view.LayoutIfNeeded();

			Assert.Equal(new UIEdgeInsets(-changed.Top, -changed.Left, -changed.Bottom, -changed.Right), controller.AdditionalSafeAreaInsets);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task LayingOutAndMovingToTheWindowAgainKeepTheSameParentViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new AppleInsetViewController(parentInsets);
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			parent.View!.AddSubview(view);

			view.SetNeedsLayout();
			view.LayoutIfNeeded();
			view.MovedToWindow();
			view.MovedToSuperview();

			Assert.Same(parent, controller.ParentViewController);
			Assert.Equal(new UIViewController?[] { parent }, controller.MovedToParents);
			Assert.Equal(new UIEdgeInsets(-10, -20, -30, -40), controller.AdditionalSafeAreaInsets);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task MovingToAnotherViewControllerMovesThePlayerViewControllerToIt()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var first = new AppleInsetViewController(parentInsets);
			var second = new AppleInsetViewController(new UIEdgeInsets(1, 2, 3, 4));
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			first.View!.AddSubview(view);

			second.View!.AddSubview(view);

			Assert.Same(second, controller.ParentViewController);
			Assert.DoesNotContain(controller, first.ChildViewControllers);
			Assert.Contains(controller, second.ChildViewControllers);
			Assert.Equal(new UIViewController?[] { first, null, second }, controller.MovedToParents);
			Assert.Equal(new UIEdgeInsets(-1, -2, -3, -4), controller.AdditionalSafeAreaInsets);

			// The player view is back in the MauiMediaElement, at its size.
			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Assert.Equal(view.Bounds, controller.View!.Frame);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task ShowingThePlaybackControlsAttachesThePlayerViewControllerAgain()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new UIViewController();
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement()) { Frame = new CGRect(0, 0, 120, 60) };
			parent.View!.AddSubview(view);

			view.RefreshPlaybackControlsVisibility(true);

			Assert.Same(parent, controller.ParentViewController);
			Assert.Equal(new UIViewController?[] { parent, null, parent }, controller.MovedToParents);
			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Assert.Equal(new CGRect(0, 0, 120, 60), controller.View!.Frame);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task HidingThePlaybackControlsLeavesThePlayerViewControllerAlone()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new UIViewController();
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			parent.View!.AddSubview(view);

			view.RefreshPlaybackControlsVisibility(false);

			Assert.Same(parent, controller.ParentViewController);
			Assert.Equal(new UIViewController?[] { parent }, controller.MovedToParents);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task ShowingThePlaybackControlsOutsideOfAnyViewControllerAddsNoParentViewController()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			view.RefreshPlaybackControlsVisibility(true);

			Assert.Null(controller.ParentViewController);
			Assert.Empty(controller.MovedToParents);
			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task DetachFromParentViewControllerRemovesThePlayerViewControllerAndItsView()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new UIViewController();
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());
			parent.View!.AddSubview(view);

			view.DetachFromParentViewController();

			Assert.Null(controller.ParentViewController);
			Assert.DoesNotContain(controller, parent.ChildViewControllers);
			Assert.Equal(new UIViewController?[] { parent, null }, controller.MovedToParents);
			Assert.Null(controller.View!.Superview);
			Assert.Empty(view.Subviews);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task DetachFromParentViewControllerWithoutParentDoesNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var controller = new AppleParentTrackingPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement());

			view.DetachFromParentViewController();

			Assert.Null(controller.ParentViewController);
			Assert.Empty(controller.MovedToParents);
			Assert.Same(controller.View, Assert.Single(view.Subviews));
			Cleanup(view, controller);
		});
	}

	// MediaElementHandler.Dispose (and DisconnectHandler) dispose the AVPlayerViewController while the view can still be in a view hierarchy:
	// the layout and the moves that UIKit still reports must not use the disposed controller (an exception there ends the app).
	[Fact]
	public async Task LayoutAndMovesAfterThePlayerViewControllerWasDisposedDoNothing()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var parent = new UIViewController();
			var other = new UIViewController();
			var controller = new AVPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement()) { Frame = new CGRect(0, 0, 120, 60) };
			parent.View!.AddSubview(view);
			controller.WillMoveToParentViewController(null);
			controller.RemoveFromParentViewController();
			controller.Dispose();

			var exception = Record.Exception(() =>
			{
				view.SetNeedsLayout();
				view.LayoutIfNeeded();
				other.View!.AddSubview(view);
				view.RefreshPlaybackControlsVisibility(true);
				view.DetachFromParentViewController();
			});

			Assert.Null(exception);
			Assert.Empty(other.ChildViewControllers);
			view.RemoveFromSuperview();
			view.Dispose();
		});
	}

	[Fact]
	public async Task LayoutSubviewsSizesThePlayerViewToTheBounds()
	{
		SetupApple();

		await InvokeOnMainThreadAsync(() =>
		{
			var controller = new AVPlayerViewController();
			var view = new MauiMediaElement(controller, CreateMediaElement()) { Frame = new CGRect(10, 20, 120, 60) };

			view.SetNeedsLayout();
			view.LayoutIfNeeded();

			Assert.Equal(new CGRect(0, 0, 120, 60), controller.View!.Frame);
			Cleanup(view, controller);
		});
	}

	[Fact]
	public async Task MauiMediaElementDoesNotLeak()
	{
		SetupApple();
		WeakReference viewReference = null!, controllerReference = null!, parentControllerReference = null!, mediaElementReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var parentController = new UIViewController();
			var mediaElement = CreateMediaElement();
			var controller = new AVPlayerViewController();
			var view = new MauiMediaElement(controller, mediaElement);
			parentController.View!.AddSubview(view);
			Assert.Same(parentController, controller.ParentViewController);
			viewReference = new(view);
			controllerReference = new(controller);
			parentControllerReference = new(parentController);
			mediaElementReference = new(mediaElement);

			view.DetachFromParentViewController();
			view.RemoveFromSuperview();
			view.Dispose();
			controller.Dispose();
		});

		await AssertionExtensions.WaitForGC(viewReference, controllerReference, parentControllerReference, mediaElementReference);
	}
}