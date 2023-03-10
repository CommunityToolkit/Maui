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

		((MauiPopup)Handler.PlatformView).SetUpPlatformView();
	}
}