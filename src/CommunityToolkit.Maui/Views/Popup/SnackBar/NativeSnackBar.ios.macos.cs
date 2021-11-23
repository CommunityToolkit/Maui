using System;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeSnackBar : NativeToast
{
	public Action Action { get; set; } = () => { };
	public NativeSnackBar()
	{
		var actionButton = new UIButton();
		actionButton.SetTitle("", UIControlState.Normal);
		PopupView.AddChild(actionButton);
	}
}
