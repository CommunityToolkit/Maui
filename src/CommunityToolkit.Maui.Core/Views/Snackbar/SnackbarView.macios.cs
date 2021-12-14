using System;
using CoreGraphics;
using ObjCRuntime;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// UIView for Snackbar on iOS
/// </summary>
public class SnackbarView : ToastView, IDisposable
{
	readonly PaddedButton _actionButton;

	bool _isDisposed;

	/// <summary>
	/// Initialize <see cref="SnackbarView"/>
	/// </summary>
	/// <param name="message">Snackbar Message Text</param>
	/// <param name="backgroundColor">Snackbar Background Color</param>
	/// <param name="cornerRadius">Snackbar Corner Radius</param>
	/// <param name="textColor">Snackbar Text Color</param>
	/// <param name="textFont">Snackbar Text Font</param>
	/// <param name="characterSpacing">Snackbar Character Spacing</param>
	/// <param name="actionButtonText">Snackbar Action Button Text</param>
	/// <param name="actionTextColor">Snackbar Action Button Text Color</param>
	/// <param name="actionButtonFont">Snackbar Action Button Font</param>
	/// <param name="padding">Snackbar Padding</param>
	public SnackbarView(
		string message,
		UIColor backgroundColor,
		CGRect cornerRadius,
		UIColor textColor,
		UIFont textFont,
		double characterSpacing,
		string actionButtonText,
		UIColor actionTextColor,
		UIFont actionButtonFont,
		nfloat? padding = null)
		: base(message, backgroundColor, cornerRadius, textColor, textFont, characterSpacing, padding)
	{
		padding ??= DefaultPadding;

		_actionButton = new PaddedButton(padding.Value, padding.Value, padding.Value, padding.Value);
		ActionButtonText = actionButtonText;
		ActionTextColor = actionTextColor;
		ActionButtonFont = actionButtonFont;

		_actionButton.TouchUpInside += ActionButton_TouchUpInside;
		PopupView.AddChild(_actionButton);
	}

	/// <summary>
	/// Finalizer for <see cref="SnackbarView"/>
	/// </summary>
	~SnackbarView() => Dispose(false);

	/// <summary>
	/// Action that executes when the user clicks the Snackbar Action Button 
	/// </summary>
	public Action? Action { get; init; }

	/// <summary>
	/// Text Displayed on Action Button
	/// </summary>
	public string ActionButtonText
	{
		get => _actionButton.Title(UIControlState.Normal);
		private init => _actionButton.SetTitle(value, UIControlState.Normal);
	}

	/// <summary>
	/// Action Button Text Color
	/// </summary>
	public UIColor ActionTextColor
	{
		get => _actionButton.TitleColor(UIControlState.Normal);
		private init => _actionButton.SetTitleColor(value, UIControlState.Normal);
	}

	/// <summary>
	/// Action Button Font
	/// </summary>
	public UIFont ActionButtonFont
	{
		get => _actionButton.Font;
		private init => _actionButton.Font = value;
	}

	/// <summary>
	/// Dispose <see cref="SnackbarView"/>
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Dispose <see cref="SnackbarView"/>
	/// </summary>
	/// <param name="isDisposing"></param>
	protected virtual void Dispose(bool isDisposing)
	{
		if (_isDisposed)
			return;

		_actionButton.TouchUpInside -= ActionButton_TouchUpInside;

		_isDisposed = true;
	}

	void ActionButton_TouchUpInside(object? sender, EventArgs e)
	{
		Action?.Invoke();
		PopupView.Dismiss();
	}
}