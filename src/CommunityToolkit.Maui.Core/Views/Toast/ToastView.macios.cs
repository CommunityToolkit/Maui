using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreGraphics;
using CoreText;
using Foundation;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Toast for iOS + MacCatalyst
/// </summary>
public class ToastView : Popup
{
	/// <summary>
	/// Default Padding for <see cref="ToastView"/>
	/// </summary>
	public const double DefaultPadding = 10;

	readonly PaddedLabel _messageLabel;

	/// <summary>
	/// Initialize <see cref="ToastView"/>
	/// </summary>
	/// <param name="message">Toast Message</param>
	/// <param name="backgroundColor">Toast Background Color</param>
	/// <param name="cornerRadius">Toast Border Corner Radius</param>
	/// <param name="textColor">Toast Text Color</param>
	/// <param name="font">Toast Font</param>
	/// <param name="characterSpacing">Toast Message Character Spacing</param>
	/// <param name="padding">Toast Padding</param>
	public ToastView(
		string message,
		UIColor backgroundColor,
		CGRect cornerRadius,
		UIColor textColor,
		UIFont font,
		double characterSpacing,
		double padding = DefaultPadding)
	{
		_messageLabel = new PaddedLabel(padding, padding, padding, padding)
		{
			Lines = 10
		};

		Message = message;
		TextColor = textColor;
		Font = font;
		CharacterSpacing = characterSpacing;
		PopupView.VisualOptions.BackgroundColor = backgroundColor;
		PopupView.VisualOptions.CornerRadius = cornerRadius;
		PopupView.AddChild(_messageLabel);
	}

	/// <summary>
	/// Toast Message
	/// </summary>
	public string? Message
	{
		get => _messageLabel.Text;
		private init => _messageLabel.Text = value;
	}

	/// <summary>
	/// Toast Text Color
	/// </summary>
	public UIColor? TextColor
	{
		get => _messageLabel.TextColor;
		private init => _messageLabel.TextColor = value;
	}

	/// <summary>
	/// Toast Font
	/// </summary>
	public UIFont Font
	{
		get => _messageLabel.Font;
		private init => _messageLabel.Font = value;
	}

	/// <summary>
	/// Toast CharacterSpacing
	/// </summary>
	public double CharacterSpacing
	{
		init
		{
			var em = GetEmFromPx(Font.PointSize, value);
			_messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)em });
		}
	}

	static nfloat GetEmFromPx(nfloat defaultFontSize, double currentValue) => 100 * (nfloat)currentValue / defaultFontSize;

	class PaddedLabel : UILabel
	{
		public PaddedLabel(double left, double top, double right, double bottom)
		{
			Left = (nfloat)left;
			Top = (nfloat)top;
			Right = (nfloat)right;
			Bottom = (nfloat)bottom;
		}

		public nfloat Left { get; }

		public nfloat Top { get; }

		public nfloat Right { get; }

		public nfloat Bottom { get; }

		public override CGSize IntrinsicContentSize => new(
			base.IntrinsicContentSize.Width + Left + Right,
			base.IntrinsicContentSize.Height + Top + Bottom);

		public override void DrawText(CGRect rect)
		{
			var insets = new UIEdgeInsets(Top, Left, Bottom, Right);
			base.DrawText(insets.InsetRect(rect));
		}
	}
}