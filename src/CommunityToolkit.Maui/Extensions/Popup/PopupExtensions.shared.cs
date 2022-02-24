using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;

namespace Microsoft.Maui.Controls;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	/// <summary>
	/// Displays a popup.
	/// </summary>
	/// <param name="page">
	/// The current <see cref="Page"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="BasePopup"/> to display.
	/// </param>
	public static void ShowPopup<TPopup>(this Page page, TPopup popup) where TPopup : BasePopup
		=> ShowPopup(page.Navigation, popup);

	/// <summary>
	/// Displays a popup.
	/// </summary>
	/// <param name="navigation">
	/// The current <see cref="INavigation"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="BasePopup"/> to display.
	/// </param>
	public static void ShowPopup<TPopup>(this INavigation navigation, TPopup popup) where TPopup : BasePopup
	{
#if WINDOWS
		PlatformShowPopup(popup, GetMauiContext(navigation));
#else
		CreatePopup(navigation, popup);
#endif

	}

	/// <summary>
	/// Displays a popup and returns a result.
	/// </summary>
	/// <param name="page">
	/// The current <see cref="Page"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="Popup"/> to display.
	/// </param>
	/// <returns>
	/// A task that will complete once the <see cref="Popup"/> is dismissed.
	/// </returns>
	public static Task<object?> ShowPopupAsync<TPopup>(this Page page, TPopup popup) where TPopup : Popup
		=> ShowPopupAsync(page.Navigation, popup);

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
	public static Task<object?> ShowPopupAsync<TPopup>(this INavigation navigation, TPopup popup) where TPopup : Popup
	{
#if WINDOWS
		return PlatformShowPopupAsync(popup, GetMauiContext(navigation));
#else

		CreatePopup(navigation, popup);
		return popup.Result;
#endif
	}

	static void CreatePopup(INavigation navigation, BasePopup popup)
	{
		var mauiContext = GetMauiContext(navigation);
		var popupNative = popup.ToHandler(mauiContext);
		popupNative.Invoke(nameof(IPopup.OnOpened));
	}

	static IMauiContext GetMauiContext(INavigation navigation)
	{
		return (Shell.Current is null ?
			navigation.NavigationStack[0].Handler?.MauiContext
			: Shell.Current.Handler?.MauiContext) ?? throw new InvalidOperationException("Could locate MauiContext");
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.BasePopup");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.Popup.");
}
#endif