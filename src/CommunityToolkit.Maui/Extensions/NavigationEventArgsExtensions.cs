using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="NavigatedFromEventArgs"/>, <see cref="NavigatedToEventArgs"/>, <see cref="NavigatingFromEventArgs"/> and <see cref="NavigatedFromEventArgs"/>.
/// </summary>
public static class NavigationEventArgsExtensions
{
	/// <summary>
	/// Determines if the destination page is a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatedFromEventArgs"/>.</param>
	/// <returns>A boolean indicating if the destination page is a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool IsDestinationPageACommunityToolkitPopupPage(this NavigatedFromEventArgs args) => args.DestinationPage is PopupPage;
	
	/// <summary>
	/// Determines whether the previous page was a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatedToEventArgs"/>.</param>
	/// <returns>A boolean indicating whether the previous page was a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool WasPreviousPageACommunityToolkitPopupPage(this NavigatedToEventArgs args) => args.PreviousPage is PopupPage;
	
	/// <summary>
	/// Determines if the destination page will be a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatingFromEventArgs"/>.</param>
	/// <returns>A boolean indicating if the destination page will be a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool IsDestinationPageACommunityToolkitPopupPage(this NavigatingFromEventArgs args) => args.DestinationPage is PopupPage;
}