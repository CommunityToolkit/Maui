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

		PopupView.ParentView?.AddSubview(PopupView);
		PopupView.ParentView?.BringSubviewToFront(PopupView);
	}

	NSTimer? timer;

	public TimeSpan Duration { get; set; }


	public UIView? Anchor { get; set; }

	protected PopupView PopupView { get; }

	public CGRect CornerRadius { get; set; }

	public UIColor BackgroundColor { get; set; } = UIColor.Gray;

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

		PopupView.Setup(CornerRadius, BackgroundColor);

		timer = NSTimer.CreateScheduledTimer(Duration, t =>
		{
			Dismiss();
		});

		return this;
	}
}