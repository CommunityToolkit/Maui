using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel) 
		: base(multiplePopupViewModel)
	{
		InitializeComponent();

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

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		var transparentPopup = new TransparentPopup();
		await Navigation.ShowPopupAsync(transparentPopup);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var openedEventSimplePopup = new OpenedEventSimplePopup();
		await Navigation.ShowPopupAsync(openedEventSimplePopup);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var returnResultPopup = new ReturnResultPopup();
		await Navigation.ShowPopupAsync(returnResultPopup);
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup();
		await Navigation.ShowPopupAsync(xamlBindingPopup);
	}

	async void HandleCsharpBindingPopupButtonClicked(object sender, EventArgs e)
	{
		var csharpBindingPopup = new CsharpBindingPopup();
		await Navigation.ShowPopupAsync(csharpBindingPopup);
	}
}