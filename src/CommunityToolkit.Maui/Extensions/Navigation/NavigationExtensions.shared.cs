
using System.Threading.Tasks;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

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
	public static void ShowPopup(this INavigation navigation, BasePopup popup)
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
	/// <param name="navigation">
	/// The current <see cref="INavigation"/>.
	/// </param>
	/// <param name="popup">
	/// The <see cref="Popup"/> to display.
	/// </param>
	/// <returns>
	/// A task that will complete once the <see cref="Popup"/> is dismissed.
	/// </returns>
	public static Task<object?> ShowPopupAsync(this INavigation navigation, Popup popup)
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
			: Shell.Current.Handler?.MauiContext) ?? throw new NullReferenceException(nameof(MauiContext));
	}
}

#if !(ANDROID || IOS || MACCATALYST || WINDOWS)
public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.BasePopup");

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext) =>
		throw new NotSupportedException($"The current platform '{Device.RuntimePlatform}' does not support CommunityToolkit.Maui.Core.Popup.");
}
#endif