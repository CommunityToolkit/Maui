using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup;

class Popup
{
	public Popup()
	{
		PopupView = new PopupView();

		PopupView.ParentView.AddSubview(PopupView);
		PopupView.ParentView.BringSubviewToFront(PopupView);
	}

	NSTimer? timer;

	public TimeSpan Duration { get; set; }

	public UIView? Anchor { get; set; }

	protected PopupView PopupView { get; }

	public void Dismiss()
	{
		if (timer != null)
		{
			timer.Invalidate();
			timer.Dispose();
			timer = null;
		}

		PopupView.Dismiss();
	}

	public void Show()
	{
		PopupView.AnchorView = Anchor;

		PopupView.Setup();

		timer = NSTimer.CreateScheduledTimer(Duration, t =>
		{
			Dismiss();
		});
	}
}