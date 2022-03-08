using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel)
		: base(multiplePopupViewModel)
	{
		InitializeComponent();
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var simplePopup = new SimplePopup();
		await simplePopup.ShowPopupAsync();
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var buttonPopup = new ButtonPopup();
		await buttonPopup.ShowPopupAsync();
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var multipleButtonPopup = new MultipleButtonPopup();
		await multipleButtonPopup.ShowPopupAsync();
	}

	async void HandleNoLightDismissPopupButtonClicked(object sender, EventArgs e)
	{
		var noLightDismissPopup = new NoLightDismissPopup();
		await noLightDismissPopup.ShowPopupAsync();
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		var toggleSizePopup = new ToggleSizePopup();
		await toggleSizePopup.ShowPopupAsync();
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		var transparentPopup = new TransparentPopup();
		await transparentPopup.ShowPopupAsync();
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var openedEventSimplePopup = new OpenedEventSimplePopup();
		await openedEventSimplePopup.ShowPopupAsync();
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var returnResultPopup = new ReturnResultPopup();
		var result = await returnResultPopup.ShowPopupAsync();

		await DisplayAlert("Pop Result Returned", $"Result: {result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup();
		await xamlBindingPopup.ShowPopupAsync();
	}

	async void HandleCsharpBindingPopupButtonClicked(object sender, EventArgs e)
	{
		var csharpBindingPopup = new CsharpBindingPopup();
		await csharpBindingPopup.ShowPopupAsync();
	}
}