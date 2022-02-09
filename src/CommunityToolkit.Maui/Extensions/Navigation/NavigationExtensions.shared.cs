
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Extensions;

/// <summary>
/// Extension methods for <see cref="INavigation"/>.
/// </summary>
public static partial class NavigationExtensions
{
	/// <summary>
	/// Displays a popup.
	/// </summary>
	/// <param name="navigation">
	/// The current <see cref="INavigation"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="BasePopup"/> to display.
	/// </param>
	public static void ShowPopup(this INavigation navigation, BasePopup popup) =>
		PlatformShowPopup(popup, navigation.NavigationStack[0].Handler?.MauiContext ?? throw new NullReferenceException(nameof(MauiContext)));

	/// <summary>
	/// Displays a popup and returns a result.
	/// </summary>
	/// <param name="navigation">
	/// The current <see cref="INavigation"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="Popup"/> to display.
	/// </param>
	/// <returns>
	/// A task that will complete once the <see cref="Popup"/> is dismissed.
	/// </returns>
	public static Task<object?> ShowPopupAsync(this INavigation navigation, Popup popup) =>
		PlatformShowPopupAsync(popup, navigation.NavigationStack[0].Handler?.MauiContext ?? throw new NullReferenceException(nameof(MauiContext)));
}