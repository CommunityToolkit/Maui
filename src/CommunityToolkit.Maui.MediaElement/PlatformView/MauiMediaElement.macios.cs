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

		// This shouldn't be necessary but something in this call is making the playback controls largely size correctly.
		// Largely because they are slightly off at the top margin, but if you fullscreen then cancel fullscreen they size correctly.
		var vc = WindowStateManager.Default.GetCurrentUIViewController() ?? throw new NullReferenceException("ViewController can't be null.");
		_ = vc.View ?? throw new NullReferenceException(nameof(vc.View));
		vc.View.AddSubview(playerViewController.View);

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