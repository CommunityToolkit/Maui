using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MultiplePopupViewModel(IPopupService popupService) : BaseViewModel
{
	[RelayCommand]
	Task OnCsharpBindingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<CsharpBindingPopupViewModel>(Application.Current!.Windows[0].Page!.Navigation, new PopupOptions(), token);
	}

	[RelayCommand]
	Task OnUpdatingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<UpdatingPopupViewModel>(Application.Current!.Windows[0].Page!.Navigation, new PopupOptions(), token);
	}

	[RelayCommand]
	Task OnShowPopupContent(CancellationToken token)
	{
		return popupService.ShowPopupAsync<PopupContentViewModel>(Application.Current!.Windows[0].Page!.Navigation, new PopupOptions(), token);
	}
}