using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	readonly PopupSizeConstants popupSizeConstants;
	readonly CsharpBindingPopupViewModel csharpBindingPopupViewModel;

	public MultiplePopupPage(IDeviceInfo deviceInfo,
								PopupSizeConstants popupSizeConstants,
								MultiplePopupViewModel multiplePopupViewModel,
								CsharpBindingPopupViewModel csharpBindingPopupViewModel)
		: base(deviceInfo, multiplePopupViewModel)
	{
		InitializeComponent();

		this.popupSizeConstants = popupSizeConstants;
		this.csharpBindingPopupViewModel = csharpBindingPopupViewModel;
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var simplePopup = new SimplePopup(popupSizeConstants);
		await this.ShowPopupAsync(simplePopup);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var buttonPopup = new ButtonPopup(popupSizeConstants);
		await this.ShowPopupAsync(buttonPopup);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var multipleButtonPopup = new MultipleButtonPopup(popupSizeConstants);
		await this.ShowPopupAsync(multipleButtonPopup);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		var noOutsideTapDismissPopup = new NoOutsideTapDismissPopup(popupSizeConstants);
		await this.ShowPopupAsync(noOutsideTapDismissPopup);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		var toggleSizePopup = new ToggleSizePopup(popupSizeConstants);
		await this.ShowPopupAsync(toggleSizePopup);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		var transparentPopup = new TransparentPopup();
		await this.ShowPopupAsync(transparentPopup);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var openedEventSimplePopup = new OpenedEventSimplePopup(popupSizeConstants);
		await this.ShowPopupAsync(openedEventSimplePopup);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var returnResultPopup = new ReturnResultPopup(popupSizeConstants);
		var result = await this.ShowPopupAsync(returnResultPopup);

		await DisplayAlert("Pop Result Returned", $"Result: {result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup(popupSizeConstants);
		await this.ShowPopupAsync(xamlBindingPopup);
	}

	async void HandleCsharpBindingPopupButtonClicked(object sender, EventArgs e)
	{
		var csharpBindingPopup = new CsharpBindingPopup(popupSizeConstants, csharpBindingPopupViewModel);
		await this.ShowPopupAsync(csharpBindingPopup);
	}
}