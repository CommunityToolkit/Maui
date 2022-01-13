using System;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Extensions;

public static partial class NavigationExtensions
{
	static void PlatformShowPopup(BasePopup popup, IMauiContext mauiContext)
	{
		var window = mauiContext.GetNativeWindow().GetWindow();
		popup.Parent = GetCurrentPage((Page)window!.Content);
		CreateRenderer(popup, mauiContext);
		// https://github.com/xamarin/Xamarin.Forms/blob/0c95d0976cc089fe72476fb037851a64987de83c/Xamarin.Forms.Platform.iOS/PageExtensions.cs#L44
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

	static Task<T?> PlatformShowPopupAsync<T>(Popup<T> popup, IMauiContext mauiContext)
	{
		PlatformShowPopup(popup, mauiContext);
		return popup.Result;
	}

	/// <summary>
	/// ATTENTION: Create the Renderer for UWP Don't use the one Provided by Xamarin.Forms, Causes a crash in Native Compiled Code
	/// 1. DefaultRenderer is PopupRenderer instead of DefaultRenderer()
	/// 2. No Invalid Cast Exceptions in UWP Native when the Xamarin Forms Renderer Functions is used.
	/// </summary>
	/// <param name="element">Element for getting the renderer</param>
	/// <param name="mauiContext"></param>
	// https://github.com/xamarin/Xamarin.Forms/blob/5.0.0/Xamarin.Forms.Platform.UAP/Platform.cs
	static void CreateRenderer(Element element, IMauiContext mauiContext)
	{
		if (element == null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		_ = element.ToNative(mauiContext);
	}
}
