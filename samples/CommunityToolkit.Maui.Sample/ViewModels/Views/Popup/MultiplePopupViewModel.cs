using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MultiplePopupViewModel(IPopupService popupService) : BaseViewModel
{
	[RelayCommand]
	Task OnCsharpBindingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<CsharpBindingPopupViewModel>(new PopupOptions(), token);
	}

	[RelayCommand]
	Task OnUpdatingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<UpdatingPopupViewModel>(new PopupOptions(), token);
	}

	[RelayCommand]
	Task OnShowPopupContent(CancellationToken token)
	{
		return popupService.ShowPopupAsync<PopupContentViewModel>(new PopupOptions(), token);
	}
}