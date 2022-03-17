using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.UI.Xaml.Controls;

namespace CommunityToolkit.Maui.Views;

public partial class Popup
{
	void OnPopupHandlerChanged(object? sender, EventArgs e)
	{
		if (Handler?.PlatformView is null)
		{
			return;
		}

		((MauiPopup)Handler.PlatformView).SetUpPlatformView(CleanUp, CreateWrapperContent);

		static void CleanUp(Panel wrapper)
		{
			((WrapperControl)wrapper).CleanUp();
		}

		static Panel? CreateWrapperContent(PopupHandler handler)
		{
			if (handler.VirtualView.Content is null || handler.MauiContext is null)
			{
				return null;
			}

			return new WrapperControl((View)handler.VirtualView.Content, handler.MauiContext);
		}
	}
}
