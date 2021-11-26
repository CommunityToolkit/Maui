using System;
using CommunityToolkit.Maui.Extensions;
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
		set
		{
			var em = GetEmFromPx(Font.PointSize, value);
			messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)em });
		}
	}

	public UIFont Font
	{
		get => messageLabel.Font;
		set => messageLabel.Font = value;
	}

	readonly PaddedLabel messageLabel;
	public NativeToast(double padding = 10)
	{
		messageLabel = new PaddedLabel(padding, padding, padding, padding)
		{
			Lines = 10
		};
		PopupView.AddChild(messageLabel);
	}

	nfloat GetEmFromPx(nfloat defaultFontSize, double currentValue)
	{
		return 100 * (nfloat)currentValue / defaultFontSize;
	}
}