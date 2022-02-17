using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel) 
		: base(multiplePopupViewModel)
	{
		InitializeComponent();

		// Todo Put these views inside a page
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(ToggleSizePopup), "Toggle Size Popup", Colors.Red, "Displays a popup that can have it's size updated by pressing a button"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(TransparentPopup), "Transparent Popup", Colors.Red, "Displays a popup with a transparent background"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(OpenedEventSimplePopup), "Opened Event Popup", Colors.Red, "Popup with opened event"),
		//	SectionModel.Create<PopupGalleryViewModel>(typeof(ReturnResultPopup), "Return Result Popup", Colors.Red, "A popup that returns a string message when dismissed"),
		//	SectionModel.Create<XamlBindingPopupViewModel>(typeof(XamlBindingPopup), "Xaml Binding Popup", Colors.Red, "A simple popup that uses XAML BindingContext"),
		//	SectionModel.Create<CsharpBindingPopupViewModel>(typeof(CsharpBindingPopup), "C# Binding Popup", Colors.Red, "A simple popup that uses C# BindingContext")
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var simplePopup = new SimplePopup();
		await Navigation.ShowPopupAsync(simplePopup);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var buttonPopup = new ButtonPopup();
		await Navigation.ShowPopupAsync(buttonPopup);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var multipleButtonPopup = new MultipleButtonPopup();
		await Navigation.ShowPopupAsync(multipleButtonPopup);
	}

	async void HandleNoLightDismissPopupButtonClicked(object sender, EventArgs e)
	{
		var noLightDismissPopup = new NoLightDismissPopup();
		await Navigation.ShowPopupAsync(noLightDismissPopup);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		var toggleSizePopup = new ToggleSizePopup();
		await Navigation.ShowPopupAsync(toggleSizePopup);
	}
}