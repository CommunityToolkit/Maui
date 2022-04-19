using System.Runtime.InteropServices;
using CoreGraphics;
using CoreText;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Toast for iOS + MacCatalyst
/// </summary>
public class PlatformToast : Alert
{
	readonly PaddedLabel messageLabel;
	internal const float defaultPadding = 10;

	/// <summary>
	/// Initialize <see cref="PlatformToast"/>
	/// </summary>
	/// <param name="message">Toast Message</param>
	/// <param name="backgroundColor">Toast Background Color</param>
	/// <param name="cornerRadius">Toast Border Corner Radius</param>
	/// <param name="textColor">Toast Text Color</param>
	/// <param name="font">Toast Font</param>
	/// <param name="characterSpacing">Toast Message Character Spacing</param>
	/// <param name="padding">Toast Padding</param>
	public PlatformToast(
		string message,
		UIColor backgroundColor,
		CGRect cornerRadius,
		UIColor textColor,
		UIFont font,
		double characterSpacing,
		NFloat padding)
	{
		padding += defaultPadding;

		messageLabel = new PaddedLabel(padding, padding, padding, padding)
		{
			Lines = 0
		};

		Message = message;
		TextColor = textColor;
		Font = font;
		CharacterSpacing = characterSpacing;
		AlertView.VisualOptions.BackgroundColor = backgroundColor;
		AlertView.VisualOptions.CornerRadius = cornerRadius;
		AlertView.AddChild(messageLabel);
	}
	
	/// <summary>
	/// Toast Message
	/// </summary>
	public string Message
	{
		get => messageLabel.Text ??= string.Empty;
		private init => messageLabel.Text = value;
	}

	/// <summary>
	/// Toast Text Color
	/// </summary>
	public UIColor TextColor
	{
		get => messageLabel.TextColor ??= AlertDefaults.TextColor.ToPlatform();
		private init => messageLabel.TextColor = value;
	}

	/// <summary>
	/// Toast Font
	/// </summary>
	public UIFont Font
	{
		get => messageLabel.Font;
		private init => messageLabel.Font = value;
	}

	/// <summary>
	/// Toast CharacterSpacing
	/// </summary>
	public double CharacterSpacing
	{
		init
		{
			var em = Font.PointSize > 0 ? GetEmFromPx(Font.PointSize, value) : 0;
			messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)em });
		}
	}

	static NFloat GetEmFromPx(NFloat defaultFontSize, double currentValue) => 100 * (NFloat)currentValue / defaultFontSize;
}