using System.Runtime.InteropServices;
using CoreGraphics;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// UIView for Snackbar on iOS
/// </summary>
public class SnackbarView : ToastView, IDisposable
{
	readonly PaddedButton actionButton;

	bool isDisposed;

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
		NFloat? padding = null)
		: base(message, backgroundColor, cornerRadius, textColor, textFont, characterSpacing, padding)
	{
		padding ??= DefaultPadding;

		actionButton = new PaddedButton(padding.Value, padding.Value, padding.Value, padding.Value);
		ActionButtonText = actionButtonText;
		ActionTextColor = actionTextColor;
		ActionButtonFont = actionButtonFont;

		actionButton.TouchUpInside += ActionButton_TouchUpInside;
		AlertView.AddChild(actionButton);
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
		get => actionButton.Title(UIControlState.Normal);
		private init => actionButton.SetTitle(value, UIControlState.Normal);
	}

	/// <summary>
	/// Action Button Text Color
	/// </summary>
	public UIColor ActionTextColor
	{
		get => actionButton.TitleColor(UIControlState.Normal);
		private init => actionButton.SetTitleColor(value, UIControlState.Normal);
	}

	/// <summary>
	/// Action Button Font
	/// </summary>
	public UIFont ActionButtonFont
	{
		get => actionButton.TitleLabel.Font;
		private init => actionButton.TitleLabel.Font = value;
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
		if (isDisposed)
		{
			return;
		}

		actionButton.TouchUpInside -= ActionButton_TouchUpInside;

		isDisposed = true;
	}

	void ActionButton_TouchUpInside(object? sender, EventArgs e)
	{
		Action?.Invoke();
		AlertView.Dismiss();
	}
}