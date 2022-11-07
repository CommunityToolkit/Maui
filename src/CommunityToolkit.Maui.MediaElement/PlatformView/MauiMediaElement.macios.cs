using System.Diagnostics;
using AVFoundation;
using AVKit;
using CoreMedia;
using Foundation;
using Microsoft.Maui.Controls;
using UIKit;

namespace CommunityToolkit.Maui.MediaElement.PlatformView;


public class MauiMediaElement : UIView
{
	protected NSObject? playedToEndObserver;

	public MauiMediaElement(AVPlayerViewController playerViewController)
	{
		_ = playerViewController.View ?? throw new NullReferenceException(nameof(playerViewController.View));
		playerViewController.View.Frame = this.Bounds;

#if IOS16_0_OR_GREATER
		// On iOS 16 the AVPlayerViewController has to be added to the parent ViewController, otherwise the transport controls won't be displayed.
		var viewController = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new NullReferenceException("ViewController can't be null.");

		_ = viewController.View ?? throw new NullReferenceException(nameof(viewController.View));

		// Zero out the safe area insets of the AVPlayerViewController
		UIEdgeInsets insets = viewController.View.SafeAreaInsets;
		playerViewController.AdditionalSafeAreaInsets = new UIEdgeInsets(insets.Top * -1, insets.Left, insets.Bottom * -1, insets.Right);

		// Add the View from the AVPlayerViewController to the parent ViewController
		viewController.View.AddSubview(playerViewController.View);
#endif

		AddSubview(playerViewController.View);
	}

	protected override void Dispose(bool disposing)
	{
		//if (disposing)
		//{
		//	if (player != null)
		//	{
		//		player.ReplaceCurrentItemWithPlayerItem(null);
		//		player.Dispose();
		//	}

		//	if (playerViewController != null)
		//	{
		//		playerViewController.Dispose();
		//	}

		//	mediaElement = null;
		//}

		base.Dispose(disposing);
	}
}