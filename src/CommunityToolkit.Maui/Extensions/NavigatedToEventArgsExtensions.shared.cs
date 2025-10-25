using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="NavigatedToEventArgs"/>.
/// </summary>
public static class NavigatedToEventArgsExtensions
{
	/// <summary>
	/// Determines whether the previous page was a Community Toolkit <see cref="Popup"/>.
	/// </summary>
	/// <param name="args">The current <see cref="NavigatedToEventArgs"/>.</param>
	/// <returns>A boolean indicating whether the previous page was a Community Toolkit <see cref="Popup"/>.</returns>
	public static bool WasPreviousPageACommunityToolkitPopupPage(this NavigatedToEventArgs args) => args.PreviousPage is PopupPage;
}