using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	readonly CsharpBindingPopupViewModel csharpBindingPopupViewModel;

	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel, CsharpBindingPopupViewModel csharpBindingPopupViewModel)
		: base(multiplePopupViewModel)
	{
		InitializeComponent();
		this.csharpBindingPopupViewModel = csharpBindingPopupViewModel;
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

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		var noOutsideTapDismissPopup = new NoOutsideTapDismissPopup();
		await this.ShowPopupAsync(noOutsideTapDismissPopup);
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
		var result = await this.ShowPopupAsync(returnResultPopup);

		await DisplayAlert("Pop Result Returned", $"Result: {result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup();
		await this.ShowPopupAsync(xamlBindingPopup);
	}

	async void HandleCsharpBindingPopupButtonClicked(object sender, EventArgs e)
	{
		var csharpBindingPopup = new CsharpBindingPopup(csharpBindingPopupViewModel);
		await this.ShowPopupAsync(csharpBindingPopup);
	}
}