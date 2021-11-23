using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativePopup
{
	public NativePopup()
	{
		PopupView = new PopupView();
	}

	NSTimer? timer;

	public TimeSpan Duration { get; set; }


	public UIView? Anchor { get; set; }

	protected PopupView PopupView { get; }

	public CGRect CornerRadius { get; set; }

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

	public NativePopup Show()
	{
		PopupView.AnchorView = Anchor;

		PopupView.ParentView?.AddSubview(PopupView);
		PopupView.ParentView?.BringSubviewToFront(PopupView);
		PopupView.Setup(CornerRadius);

		timer = NSTimer.CreateScheduledTimer(Duration, t =>
		{
			Dismiss();
		});

		return this;
	}
}