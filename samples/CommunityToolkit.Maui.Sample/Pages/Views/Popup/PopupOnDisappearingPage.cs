using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Views.Popups;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

public class PopupOnDisappearingPage : ContentPage
{
	public PopupOnDisappearingPage()
	{
		Content = new Label().Text("A popup will be displayed when this page is dismissed").Center().TextCenter();
	}

	protected override async void OnDisappearing()
	{
		base.OnDisappearing();

		await this.ShowPopupAsync(new RedBlueBoxPopup(), new PopupOptions
		{
			Shape = null,
			Padding = 0,
		});
	}
}
