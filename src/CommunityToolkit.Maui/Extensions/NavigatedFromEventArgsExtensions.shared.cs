using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="NavigatedFromEventArgs"/>.
/// </summary>
public static class NavigatedFromEventArgsExtensions
{
	/// <summary>
	/// Determines whether the previous page was a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatedFromEventArgs"/>.</param>
	/// <returns>A boolean indicating whether the previous page was a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool IsDestinationPageACommunityToolkitPopupPage(this NavigatedFromEventArgs args) => args.DestinationPage is PopupPage;
}