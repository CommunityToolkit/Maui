using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
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

	protected override void OnAppearing()
	{
		base.OnAppearing();

		// Proves that we now support showing a popup before the platform is even ready.
		this.ShowPopup(new SimplePopup(popupSizeConstants));
	}
}
