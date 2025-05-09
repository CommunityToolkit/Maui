using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ShowPopupInOnAppearingPage : BasePage<ShowPopupInOnAppearingPageViewModel>
{
	readonly IPopupService popupService;
	bool hasPopupBeenShown;

	public ShowPopupInOnAppearingPage(
		ShowPopupInOnAppearingPageViewModel showPopupInOnAppearingPageViewModel,
		IPopupService popupService)
		: base(showPopupInOnAppearingPageViewModel)
	{
		InitializeComponent();

		this.popupService = popupService;
	}

	protected override async void OnAppearing()
	{
		if (!hasPopupBeenShown)
		{
			hasPopupBeenShown = true;
			await popupService.ShowPopupAsync<ReturnResultPopup, string>(Navigation);
		}
	}
}