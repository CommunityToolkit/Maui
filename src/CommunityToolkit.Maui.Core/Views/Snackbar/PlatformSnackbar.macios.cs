using System.Runtime.InteropServices;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// UIView for Snackbar on iOS
/// </summary>
public class PlatformSnackbar : PlatformToast
{
	readonly PaddedButton actionButton;

	/// <summary>
	/// Initialize <see cref="PlatformSnackbar"/>
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
	public PlatformSnackbar(
		string message,
		UIColor backgroundColor,
		CGRect cornerRadius,
		UIColor textColor,
		UIFont textFont,
		double characterSpacing,
		string actionButtonText,
		UIColor actionTextColor,
		UIFont actionButtonFont,
		NFloat padding)
		: base(message, backgroundColor, cornerRadius, textColor, textFont, characterSpacing, padding)
	{
		padding += DefaultPadding;

		actionButton = new PaddedButton(padding, padding, padding, padding);
		actionButton.SetContentCompressionResistancePriority((float)UILayoutPriority.Required, UILayoutConstraintAxis.Horizontal);
		actionButton.SetContentHuggingPriority((float)UILayoutPriority.Required, UILayoutConstraintAxis.Horizontal);
		ActionButtonText = actionButtonText;
		ActionTextColor = actionTextColor;
		ActionButtonFont = actionButtonFont;

		actionButton.TouchUpInside += ActionButton_TouchUpInside;
		AlertView.AddChild(actionButton);
	}

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
	/// Dispose <see cref="PlatformSnackbar"/>
	/// </summary>
	/// <param name="isDisposing"></param>
	protected override void Dispose(bool isDisposing)
	{
		if (isDisposing)
		{
			actionButton.TouchUpInside -= ActionButton_TouchUpInside;
		}

		base.Dispose(isDisposing);
	}

	void ActionButton_TouchUpInside(object? sender, EventArgs e)
	{
		Action?.Invoke();
		AlertView.Dismiss();
	}
}