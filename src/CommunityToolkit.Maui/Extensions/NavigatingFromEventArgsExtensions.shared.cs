using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="NavigatingFromEventArgs"/>.
/// </summary>
public static class NavigatingFromEventArgsExtensions
{
	/// <summary>
	/// Determines if the destination page will be a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatingFromEventArgs"/>.</param>
	/// <returns>A boolean indicating if the destination page will be a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool IsDestinationPageACommunityToolkitPopupPage(this NavigatingFromEventArgs args) => args.DestinationPage is PopupPage;
}
