using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

public class PopupOnDisappearingPage : ContentPage
{
	public PopupOnDisappearingPage()
	{
		Content = new VerticalStackLayout
		{
			Children =
			{
				new Label().Text("This is a modal page. A popup will be displayed when this modal page is dismissed").Center().TextCenter(),
				new Button
				{
					Command = new AsyncRelayCommand(() => Navigation.PopModalAsync())
				}.Text("Go Back").Center()
			}
		}.Center();
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