using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Popup for iOS + MacCatalyst
/// </summary>
public class Popup
{
	NSTimer? _timer;

	/// <summary>
	/// Initialize Popup
	/// </summary>
	public Popup()
	{
		PopupView = new PopupView();

		PopupView.ParentView.AddSubview(PopupView);
		PopupView.ParentView.BringSubviewToFront(PopupView);
	}

	/// <summary>
	/// Duration of time before <see cref="Popup"/> disappears
	/// </summary>
	public TimeSpan Duration { get; set; }

	/// <summary>
	/// <see cref="UIView"/> on which Popup will appear. When null, <see cref="Popup"/> will appear at bottom of screen.
	/// </summary>
	public UIView? Anchor { get; set; }

	/// <summary>
	/// <see cref="UIView"/> for <see cref="Popup"/>
	/// </summary>
	protected PopupView PopupView { get; }

	/// <summary>
	/// Dismiss the <see cref="Popup"/> from the screen
	/// </summary>
	public void Dismiss()
	{
		if (_timer != null)
		{
			_timer.Invalidate();
			_timer.Dispose();
			_timer = null;
		}

		PopupView.Dismiss();
	}

	/// <summary>
	/// Show the <see cref="Popup"/> on the screen
	/// </summary>
	public void Show()
	{
		PopupView.AnchorView = Anchor;

		PopupView.Setup();

		_timer = NSTimer.CreateScheduledTimer(Duration, t =>
		{
			Dismiss();
		});
	}
}