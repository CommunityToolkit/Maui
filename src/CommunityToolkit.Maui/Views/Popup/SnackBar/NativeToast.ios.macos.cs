using CoreText;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeToast : NativePopup
{
	public string? Message
	{
		get => messageLabel.Text;
		set => messageLabel.Text = value;
	}

	public UIColor? TextColor
	{
		get => messageLabel.TextColor;
		set => messageLabel.TextColor = value;
	}

	public double CharacterSpacing
	{
		set => messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)value });
	}

	public UIFont Font
	{
		get => messageLabel.Font;
		set => messageLabel.Font = value;
	}

	readonly UILabel messageLabel;
	public NativeToast()
	{
		messageLabel = new UILabel
		{
			Lines = 0,
			AdjustsFontSizeToFitWidth = true
		};
		PopupView.AddChild(messageLabel);
	}
}
