using System.Diagnostics.CodeAnalysis;
using AVKit;
using CommunityToolkit.Maui.Views;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on iOS and macOS.
/// </summary>
public class MauiMediaElement : UIView
{
	#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
	readonly AVPlayerViewController playerViewController;
	#endif
	readonly UIView playerView;
	
	/// <summary>
	/// Initializes a new instance of the <see cref="MauiMediaElement"/> class.
	/// </summary>
	/// <param name="playerViewController">The <see cref="AVPlayerViewController"/> that acts as the platform media player.</param>
	/// <param name="virtualView">The <see cref="MediaElement"/> used as the VirtualView for this <see cref="MauiMediaElement"/>.</param>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="playerViewController"/><c>.View</c> is <see langword="null"/>.</exception>
	public MauiMediaElement(AVPlayerViewController playerViewController, MediaElement virtualView)
	{
		ArgumentNullException.ThrowIfNull(virtualView);

		#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
		this.playerViewController = playerViewController;
		#endif
		playerView = playerViewController.View ?? throw new InvalidOperationException($"{nameof(playerViewController)}.{nameof(playerViewController.View)} cannot be null.");

		playerView.Frame = Bounds;
		AddSubview(playerView);
		TryAttachToParentViewController();
	}

	/// <inheritdoc/>
	public override void LayoutSubviews()
	{
		base.LayoutSubviews();
		playerView.Frame = Bounds;
		TryAttachToParentViewController();
	}

	/// <inheritdoc/>
	public override void MovedToSuperview()
	{
		base.MovedToSuperview();
		TryAttachToParentViewController();
	}

	/// <inheritdoc/>
	public override void MovedToWindow()
	{
		base.MovedToWindow();
		TryAttachToParentViewController();
	}

	/// <summary>
	/// Forces AVKit to rebuild the player view hierarchy after playback controls are enabled.
	/// </summary>
	/// <param name="shouldShowPlaybackControls"><see langword="true"/> when playback controls should be visible.</param>
	public void RefreshPlaybackControlsVisibility(bool shouldShowPlaybackControls)
	{
		if (!shouldShowPlaybackControls)
		{
			return;
		}

		TryAttachToParentViewController(forceReattach: true);

		SetNeedsLayout();
		LayoutIfNeeded();
		playerView.SetNeedsLayout();
		playerView.LayoutIfNeeded();
		playerView.SetNeedsDisplay();
	}

	void TryAttachToParentViewController(bool forceReattach = false)
	{
#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
		if (!TryGetParentViewController(out var viewController) || viewController.View is not UIView parentView)
		{
			return;
		}

		if (!forceReattach && ReferenceEquals(playerViewController.ParentViewController, viewController))
		{
			return;
		}

		if (playerViewController.ParentViewController is UIViewController previousParent)
		{
			if (playerViewController.View is UIView attachedView)
			{
				attachedView.RemoveFromSuperview();
			}

			AddSubview(playerView);
			playerView.Frame = Bounds;
			playerViewController.WillMoveToParentViewController(null);
			playerViewController.RemoveFromParentViewController();
		}

		UIEdgeInsets insets = parentView.SafeAreaInsets;
		playerViewController.AdditionalSafeAreaInsets =
			new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);

		viewController.AddChildViewController(playerViewController);
		playerViewController.DidMoveToParentViewController(viewController);
#endif
	}

	#if IOS16_0_OR_GREATER || MACCATALYST16_1_OR_GREATER
	bool TryGetParentViewController([NotNullWhen(true)] out UIViewController? viewController)
	{
		viewController = GetViewControllerFromResponderChain();
		return viewController is not null;
	}

	UIViewController? GetViewControllerFromResponderChain()
	{
		for (UIResponder? responder = NextResponder; responder is not null; responder = responder.NextResponder)
		{
			if (responder is UIViewController viewController)
			{
				return viewController;
			}
		}

		return null;
	}
	#endif
}