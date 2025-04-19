using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupsViewModel(IPopupService popupService) : BaseViewModel
{
	static INavigation currentNavigation => Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException($"{nameof(Page.Navigation)} not found");

	[RelayCommand]
	void OnCsharpBindingPopup()
	{
		popupService.ShowPopup<CsharpBindingPopupViewModel>(currentNavigation);
	}

	[RelayCommand]
	void OnUpdatingPopup()
	{
		popupService.ShowPopup<UpdatingPopupViewModel>(currentNavigation);
	}

	[RelayCommand]
	Task OnShowPopupContent(CancellationToken token)
	{
		return popupService.ShowPopupAsync<PopupContentViewModel>(currentNavigation, cancellationToken: token);
	}
}