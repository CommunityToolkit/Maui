using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	readonly PopupSizeConstants popupSizeConstants;

	public MultiplePopupPage(PopupSizeConstants popupSizeConstants,
								MultiplePopupViewModel multiplePopupViewModel)
		: base(multiplePopupViewModel)
	{
		InitializeComponent();

		this.popupSizeConstants = popupSizeConstants;
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var simplePopup = new SimplePopup(popupSizeConstants);
		await this.ShowPopupAsync(simplePopup, CancellationToken.None);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var buttonPopup = new ButtonPopup(popupSizeConstants);
		await this.ShowPopupAsync(buttonPopup, CancellationToken.None);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		var multipleButtonPopup = new MultipleButtonPopup(popupSizeConstants);
		await this.ShowPopupAsync(multipleButtonPopup, CancellationToken.None);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		var noOutsideTapDismissPopup = new NoOutsideTapDismissPopup(popupSizeConstants);
		await this.ShowPopupAsync(noOutsideTapDismissPopup, CancellationToken.None);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		var toggleSizePopup = new ToggleSizePopup(popupSizeConstants);
		await this.ShowPopupAsync(toggleSizePopup, CancellationToken.None);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		var transparentPopup = new TransparentPopup();
		await this.ShowPopupAsync(transparentPopup, CancellationToken.None);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		var openedEventSimplePopup = new OpenedEventSimplePopup(popupSizeConstants);
		await this.ShowPopupAsync(openedEventSimplePopup, CancellationToken.None);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var returnResultPopup = new ReturnResultPopup(popupSizeConstants);
		var result = await this.ShowPopupAsync(returnResultPopup, CancellationToken.None);

		await DisplayAlert("Pop Result Returned", $"Result: {result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		var xamlBindingPopup = new XamlBindingPopup(popupSizeConstants);
		await this.ShowPopupAsync(xamlBindingPopup, CancellationToken.None);
	}
}