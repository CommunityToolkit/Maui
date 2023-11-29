using Android.App;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Platform;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// Extension methods for <see cref="Popup"/>.
/// </summary>
public static partial class PopupExtensions
{
	static void PlatformShowPopup(Page page, Popup popup)
	{
		var mauiContext = GetMauiContext(page);
		popup.Parent = PageExtensions.GetCurrentPage(page);
		var platformPopup = popup.ToHandler(mauiContext);
		platformPopup.Invoke(nameof(IPopup.OnOpened));

		if (platformPopup.PlatformView is Dialog dialog &&
			platformPopup.VirtualView is IPopup pPopup &&
			pPopup.Content?.ToPlatform(mauiContext) is AView container)
		{
			Popup.SetSize(dialog, popup, pPopup, container);
		}
	}

	static Task<object?> PlatformShowPopupAsync(Page page, Popup popup)
	{
		PlatformShowPopup(page, popup);
		return popup.Result;
	}
}