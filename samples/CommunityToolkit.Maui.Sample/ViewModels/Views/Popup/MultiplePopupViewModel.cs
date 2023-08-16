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
		return this.popupService.ShowPopupAsync<CsharpBindingPopupViewModel>(
			onPresenting: viewModel => viewModel.Load("This is a platform specific popup with a .NET MAUI View being rendered. The behaviors of the popup will confirm to 100% this platform look and feel, but still allows you to use your .NET MAUI Controls."));
	}

	[RelayCommand]
	Task OnUpdatingPopup()
	{
		return this.popupService.ShowPopupAsync<UpdatingPopupViewModel>(
			onPresenting: viewModel => viewModel.PerformUpdates(10));
	}
}