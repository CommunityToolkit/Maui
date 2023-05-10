using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MultiplePopupViewModel : BaseViewModel
{
	readonly IPopupService popupService;

	public MultiplePopupViewModel(IPopupService popupService)
	{
		this.popupService = popupService;
	}

	[RelayCommand]
	Task OnCsharpBindingPopup()
	{
		return this.popupService.ShowPopupAsync<CsharpBindingPopupViewModel>();
	}
}