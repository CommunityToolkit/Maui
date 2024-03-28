using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ShowPopupInOnAppearingPage : BasePage<ShowPopupInOnAppearingPageViewModel>
{
	readonly PopupSizeConstants popupSizeConstants;

	public ShowPopupInOnAppearingPage(
		PopupSizeConstants popupSizeConstants,
		ShowPopupInOnAppearingPageViewModel showPopupInOnAppearingPageViewModel)
		: base(showPopupInOnAppearingPageViewModel)
	{
		InitializeComponent();
		this.popupSizeConstants = popupSizeConstants;
	}

	protected override async void OnAppearing()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		// Proves that we now support showing a popup before the platform is even ready.
		await this.ShowPopupAsync(new ReturnResultPopup(popupSizeConstants), cts.Token);
	}
}