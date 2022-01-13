﻿using CoreGraphics;
using CoreText;
using Foundation;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Toast for iOS + MacCatalyst
/// </summary>
public class ToastView : Popup
{
	readonly PaddedLabel messageLabel;

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
		nfloat? padding = null)
	{
		padding ??= DefaultPadding;

		messageLabel = new PaddedLabel(padding.Value, padding.Value, padding.Value, padding.Value)
		{
			Lines = 10
		};

		Message = message;
		TextColor = textColor;
		Font = font;
		CharacterSpacing = characterSpacing;
		PopupView.VisualOptions.BackgroundColor = backgroundColor;
		PopupView.VisualOptions.CornerRadius = cornerRadius;
		PopupView.AddChild(messageLabel);
	}

	/// <summary>
	/// Default Padding for <see cref="ToastView"/>
	/// </summary>
	public static nfloat DefaultPadding { get; } = 10;

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
		get => messageLabel.TextColor ??= UIColor.Black;
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
			var em = GetEmFromPx(Font.PointSize, value);
			messageLabel.AttributedText = new NSAttributedString(Message, new CTStringAttributes() { KerningAdjustment = (float)em });
		}
	}

	static nfloat GetEmFromPx(nfloat defaultFontSize, double currentValue) => 100 * (nfloat)currentValue / defaultFontSize;
}