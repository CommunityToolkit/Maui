using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

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
		await popupService.ShowPopupAsync<SimplePopup>(new PopupOptions()
		{
			Shape = new RoundRectangle() { CornerRadius = new CornerRadius(15) }
		}, CancellationToken.None);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ButtonPopup>(new PopupOptions(), CancellationToken.None);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<MultipleButtonPopup, bool>(new PopupOptions(), CancellationToken.None);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<NoOutsideTapDismissPopup>(new PopupOptions() { CanBeDismissedByTappingOutsideOfPopup = false }, CancellationToken.None);
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ToggleSizePopup>(new PopupOptions(), CancellationToken.None);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<TransparentPopup>(new PopupOptions(), CancellationToken.None);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<OpenedEventSimplePopup>(new PopupOptions(), CancellationToken.None);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var result = await popupService.ShowPopupAsync<ReturnResultPopup, string>(new PopupOptions(), CancellationToken.None);

		await DisplayAlert("Pop Result Returned", $"Result: {result.Result}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<XamlBindingPopup>(new PopupOptions(), CancellationToken.None);
	}

	async void HandlePopupPositionButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new PopupPositionPage(new PopupPositionViewModel()));
	}
	
	async void HandlePopupAnchorButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new PopupAnchorPage(DeviceInfo.Current, new PopupAnchorViewModel()));
	}
	
	async void HandlePopupLayoutAlignmentButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new PopupLayoutAlignmentPage(new PopupLayoutAlignmentViewModel()));
	}
	
	async void HandlePopupSizingIssuesButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new PopupSizingIssuesPage(new PopupSizingIssuesViewModel()));
	}
	
	async void HandleShowPopupInOnAppearingButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new ShowPopupInOnAppearingPage(new ShowPopupInOnAppearingPageViewModel(), popupService));
	}
	
	async void HandleStylePopupButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushModalAsync(new StylePopupPage(new StylePopupViewModel()));
	}
}