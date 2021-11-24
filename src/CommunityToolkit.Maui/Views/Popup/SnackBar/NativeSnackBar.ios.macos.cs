using System;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeSnackBar : NativeToast, IDisposable
{
	public Action Action { get; set; } = () => { };
	public string ActionButtonText
	{
		get
		{
			return actionButton.Title(UIControlState.Normal);
		}
		set
		{
			actionButton.SetTitle(value, UIControlState.Normal);
		}
	}

	public UIColor ActionTextColor
	{
		get
		{
			return actionButton.TitleColor(UIControlState.Normal);
		}
		set
		{
			actionButton.SetTitleColor(value, UIControlState.Normal);
		}
	}

	public UIFont ActionButtonFont
	{
		get
		{
			return actionButton.Font;
		}
		set
		{
			actionButton.Font = value;
		}
	}


	UIButton actionButton;
	public NativeSnackBar()
	{
		actionButton = new UIButton();
		actionButton.TouchUpInside += ActionButton_TouchUpInside;
		PopupView.AddChild(actionButton);
	}

	void ActionButton_TouchUpInside(object? sender, EventArgs e)
	{
		Action?.Invoke();
		PopupView.Dismiss();
	}

	public void Dispose()
	{
		actionButton.TouchUpInside -= ActionButton_TouchUpInside;
	}
}
