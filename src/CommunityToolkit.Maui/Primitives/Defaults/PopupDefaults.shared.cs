using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui;

/// <summary>
/// Default Values for <see cref="Microsoft.Maui.Controls.View"/>
/// </summary>
static class PopupDefaults
{
	/// <summary>
	/// Default value for <see cref="Microsoft.Maui.Controls.View.Margin"/> 
	/// </summary>
	public static Thickness Margin { get; } = new(30);

	/// <summary>
	/// Default value for <see cref="Microsoft.Maui.Controls.Layout.Padding"/> 
	/// </summary>
	public static Thickness Padding { get; } = new(15);

	/// <summary>
	/// Default value for <see cref="Microsoft.Maui.Controls.View.VerticalOptions"/> 
	/// </summary>
	public static LayoutOptions VerticalOptions { get; } = LayoutOptions.Center;

	/// <summary>
	/// Default value for <see cref="Microsoft.Maui.Controls.View.HorizontalOptions"/> 
	/// </summary>
	public static LayoutOptions HorizontalOptions { get; } = LayoutOptions.Center;

	/// <summary>
	/// Default value for <see cref="VisualElement.BackgroundColor"/> BackgroundColor 
	/// </summary>
	public static Color BackgroundColor { get; } = Colors.White;

	public static bool CanBeDismissedByTappingOutsideOfPopup { get; internal set; } =
		PopupDefaults.CanBeDismissedByTappingOutsideOfPopup;
}