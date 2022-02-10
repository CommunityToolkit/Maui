using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Extensions;

public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext)
	{
		var window = mauiContext.GetNativeWindow().GetWindow();
		popup.Parent = GetCurrentPage((Page)window!.Content);
		// https://github.com/xamarin/Xamarin.Forms/blob/0c95d0976cc089fe72476fb037851a64987de83c/Xamarin.Forms.Platform.iOS/PageExtensions.cs#L44
		var native = popup.ToHandler(mauiContext);
		native?.Invoke(nameof(IPopup.OnOpened));
		Page GetCurrentPage(Page currentPage)
		{
			if (currentPage.NavigationProxy.ModalStack.LastOrDefault() is Page modal)
			{
				return modal;
			}
			else if (currentPage is FlyoutPage fp)
			{
				return GetCurrentPage(fp.Detail);
			}
			else if (currentPage is Shell shell && shell.CurrentItem?.CurrentItem is IShellSectionController ssc)
			{
				return ssc.PresentedPage;
			}
			else if (currentPage is IPageContainer<Page> pc)
			{
				return GetCurrentPage(pc.CurrentPage);
			}
			else
			{
				return currentPage;
			}
		}
	}

	static Task<object?> PlatformShowPopupAsync(Popup popup, IMauiContext mauiContext)
	{
		PlatformShowPopup(popup, mauiContext);
		return popup.Result;
	}
}
