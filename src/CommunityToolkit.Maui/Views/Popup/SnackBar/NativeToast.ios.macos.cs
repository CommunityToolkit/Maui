using CoreText;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Views.Popup.SnackBar;

class NativeToast : NativePopup
{
	public string? Message
	{
		get
		{
			return messageLabel.Text;
		}
		set
		{
			messageLabel.Text = value;
		}
	}

	public UIColor? TextColor
	{
		get
		{
			return messageLabel.TextColor;
		}
		set
		{
			messageLabel.TextColor = value;
		}
	}

	public double CharacterSpacing
	{
		get
		{
			return 0;
			//return messageLabel.AttributedText.GetAttribute(NSAttributedString.Key.kern);
		}
		set
		{
			messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)value});
		}
	}

	public UIFont Font
	{
		get
		{
			return messageLabel.Font;
		}
		set
		{
			messageLabel.Font = value;
		}
	}

	UILabel messageLabel;
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
