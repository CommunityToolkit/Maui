using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeToast : NativePopup
{
	public string Message { get; set; } = string.Empty;

	public NativeToast()
	{
		var messageLabel = new UILabel
		{
			Text = Message,
			Lines = 0,
			AdjustsFontSizeToFitWidth = true
		};
		//messageLabel.BackgroundColor = SnackBar.Appearance.Background;
		//messageLabel.TextColor = SnackBar.Appearance.Foreground;
		//messageLabel.Font = SnackBar.Appearance.Font;
		PopupView.AddChild(messageLabel);
	}

}
