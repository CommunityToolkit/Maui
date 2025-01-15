using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ShowPopupInOnAppearingPage : BasePage<ShowPopupInOnAppearingPageViewModel>
{
	readonly IPopupService popupService;

	public ShowPopupInOnAppearingPage(
		ShowPopupInOnAppearingPageViewModel showPopupInOnAppearingPageViewModel,
		IPopupService popupService)
		: base(showPopupInOnAppearingPageViewModel)
	{
		this.popupService = popupService;
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		// Proves that we now support showing a popup before the platform is even ready.
		await popupService.ShowPopupAsync<ReturnResultPopup, string>(new PopupOptions(), cts.Token);
	}
}