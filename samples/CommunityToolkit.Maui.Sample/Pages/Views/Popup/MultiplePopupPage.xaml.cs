using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
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
		await popupService.ShowPopupAsync<SimplePopup>(Navigation, new PopupOptions
		{
			Shape = new RoundRectangle { CornerRadius = new CornerRadius(15) },
			BorderStroke = Colors.White,
		}, CancellationToken.None);
	}

	async void HandleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ButtonPopup>(Navigation);
	}

	async void HandleMultipleButtonPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<MultipleButtonPopup, bool>(Navigation);
	}

	async void HandleNoOutsideTapDismissPopupClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<NoOutsideTapDismissPopup>(Navigation, new PopupOptions
		{
			CanBeDismissedByTappingOutsideOfPopup = false,
		});
	}

	async void HandleToggleSizePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<ToggleSizePopup>(Navigation);
	}

	async void HandleTransparentPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<TransparentPopup>(Navigation);
	}

	async void HandleOpenedEventSimplePopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<OpenedEventSimplePopup>(Navigation);
	}

	async void HandleReturnResultPopupButtonClicked(object sender, EventArgs e)
	{
		var result = await popupService.ShowPopupAsync<ReturnResultPopup, string>(Navigation);

		await DisplayAlert("Pop Result Returned", $"Result: {result.Result ?? "Closed by tapping outside"}", "OK");
	}

	async void HandleXamlBindingPopupPopupButtonClicked(object sender, EventArgs e)
	{
		await popupService.ShowPopupAsync<XamlBindingPopup>(Navigation);
	}

	async void HandlePopupPositionButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupPositionPage(new PopupPositionViewModel()));
	}

	async void HandlePopupLayoutAlignmentButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupLayoutAlignmentPage(new PopupLayoutAlignmentViewModel()));
	}

	async void HandlePopupSizingIssuesButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PopupSizingIssuesPage(new PopupSizingIssuesViewModel()));
	}

	async void HandleShowPopupInOnAppearingButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ShowPopupInOnAppearingPage(new ShowPopupInOnAppearingPageViewModel(), popupService));
	}

	async void HandleStylePopupButtonClicked(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new StylePopupPage(new StylePopupViewModel()));
	}
}