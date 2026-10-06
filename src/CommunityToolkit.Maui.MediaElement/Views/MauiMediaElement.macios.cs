using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using AVKit;
using CommunityToolkit.Maui.Views;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The user-interface element that represents the <see cref="MediaElement"/> on iOS and macOS.
/// </summary>
public class MauiMediaElement : UIView
{
	readonly AVPlayerViewController playerViewController;
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

		this.playerViewController = playerViewController;
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

	/// <summary>
	/// Removes the player view controller from its current parent.
	/// </summary>
	/// <remarks>
	/// The player view controller is added as a child controller in <see cref="TryAttachToParentViewController"/>.
	/// When the handler is disconnected the parent still retains the child controller, so it must be removed
	/// here to avoid leaving a stale/disposed child controller in the parent view controller.
	/// </remarks>
	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
	public void DetachFromParentViewController()
	{
		if (playerViewController.ParentViewController is not null)
		{
			playerViewController.WillMoveToParentViewController(null);

			if (playerViewController.View is UIView attachedView)
			{
				attachedView.RemoveFromSuperview();
			}

			playerViewController.RemoveFromParentViewController();
		}
	}

	void TryAttachToParentViewController(bool forceReattach = false)
	{
		if (!OperatingSystem.IsIOSVersionAtLeast(16) && !OperatingSystem.IsMacCatalystVersionAtLeast(16, 1))
		{
			return;
		}

		if (!TryGetParentViewController(out var viewController) || viewController.View is not UIView parentView)
		{
			return;
		}

		if (IsAdditionalSafeAreaInsetsStale(parentView))
		{
			ApplySafeAreaInsets(parentView);
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

		ApplySafeAreaInsets(parentView);
		viewController.AddChildViewController(playerViewController);
		playerViewController.DidMoveToParentViewController(viewController);
	}

	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
	bool IsAdditionalSafeAreaInsetsStale(UIView parentView)
	{
		UIEdgeInsets expected = CreateSafeAreaInsets(parentView);
		UIEdgeInsets actual = playerViewController.AdditionalSafeAreaInsets;

		return actual.Top != expected.Top
		       || actual.Left != expected.Left
		       || actual.Bottom != expected.Bottom
		       || actual.Right != expected.Right;
	}

	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
	void ApplySafeAreaInsets(UIView parentView)
	{
		playerViewController.AdditionalSafeAreaInsets = CreateSafeAreaInsets(parentView);
	}

	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
	static UIEdgeInsets CreateSafeAreaInsets(UIView parentView)
	{
		UIEdgeInsets insets = parentView.SafeAreaInsets;
		return new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);
	}

	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
	bool TryGetParentViewController([NotNullWhen(true)] out UIViewController? viewController)
	{
		viewController = GetViewControllerFromResponderChain();
		return viewController is not null;
	}

	[SupportedOSPlatform("ios16.0")]
	[SupportedOSPlatform("maccatalyst16.1")]
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
}