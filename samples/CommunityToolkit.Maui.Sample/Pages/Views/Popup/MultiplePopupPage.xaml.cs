using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class MultiplePopupPage : BasePage<MultiplePopupViewModel>
{
	readonly IPopupService popupService;

	public MultiplePopupPage(MultiplePopupViewModel multiplePopupViewModel, IPopupService popupService)
		: base(multiplePopupViewModel)
	{
		this.popupService = popupService;
		InitializeComponent();
	}

	async void HandleSimplePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<SimplePopup>(), CancellationToken.None);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<ButtonPopup>(), CancellationToken.None);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<MultipleButtonPopup>(), CancellationToken.None);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<NoOutsideTapDismissPopup>() { CanBeDismissedByTappingOutsideOfPopup = false }, CancellationToken.None);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<ToggleSizePopup>(), CancellationToken.None);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<TransparentPopup>(), CancellationToken.None);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<OpenedEventSimplePopup>(), CancellationToken.None);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var result = await popupService.ShowPopupAsync<ReturnResultPopup, string>(new PopupOptions<ReturnResultPopup>(), CancellationToken.None);

		await DisplayAlert("Pop Result Returned", $"Result: {result.Result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync(new PopupOptions<XamlBindingPopup>(), CancellationToken.None);
	}
}