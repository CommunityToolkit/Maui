using System;
using CommunityToolkit.Maui.Extensions;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeSnackBar : NativeToast, IDisposable
{
	public Action Action { get; set; } = () => { };
	public string ActionButtonText
	{
		get => actionButton.Title(UIControlState.Normal);
		set => actionButton.SetTitle(value, UIControlState.Normal);
	}

	public UIColor ActionTextColor
	{
		get => actionButton.TitleColor(UIControlState.Normal);
		set => actionButton.SetTitleColor(value, UIControlState.Normal);
	}

	public UIFont ActionButtonFont
	{
		get => actionButton.Font;
		set => actionButton.Font = value;
	}


	PaddedButton actionButton;
	public NativeSnackBar(double padding = 10):base(padding)
	{		
		actionButton = new PaddedButton(padding, padding, padding, padding);
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
