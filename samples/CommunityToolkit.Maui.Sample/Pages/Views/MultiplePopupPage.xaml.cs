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
		await this.ShowPopupAsync(simplePopup);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var buttonPopup = new ButtonPopup();
		await this.ShowPopupAsync(buttonPopup);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var multipleButtonPopup = new MultipleButtonPopup();
		await this.ShowPopupAsync(multipleButtonPopup);
	}

	async void HandleNoLightDismissPopupButtonClicked(object sender, EventArgs e)
	{
		var noLightDismissPopup = new NoLightDismissPopup();
		await this.ShowPopupAsync(noLightDismissPopup);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		var toggleSizePopup = new ToggleSizePopup();
		await this.ShowPopupAsync(toggleSizePopup);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		var transparentPopup = new TransparentPopup();
		await this.ShowPopupAsync(transparentPopup);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var openedEventSimplePopup = new OpenedEventSimplePopup();
		await this.ShowPopupAsync(openedEventSimplePopup);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var returnResultPopup = new ReturnResultPopup();
		await this.ShowPopupAsync(returnResultPopup);
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup();
		await this.ShowPopupAsync(xamlBindingPopup);
	}

	async void HandleCsharpBindingPopupButtonClicked(object sender, EventArgs e)
	{
		var csharpBindingPopup = new CsharpBindingPopup();
		await this.ShowPopupAsync(csharpBindingPopup);
	}
}