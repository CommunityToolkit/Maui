using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popup;

public partial class PopupOnDisappearingPage : ContentPage
{
	public PopupOnDisappearingPage()
	{
		Content = new VerticalStackLayout
		{
			Spacing = 12,
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

		// RedBlueBoxPopup has no size of its own: without one its four 10x10 corner squares lie on top of each other, so give it the 100x100 of the Popup Layout page
		await this.ShowPopupAsync(new RedBlueBoxPopup
		{
			Padding = 0,
			WidthRequest = 100,
			HeightRequest = 100
		}, new PopupOptions
		{
			Shape = null,
		});
	}
}