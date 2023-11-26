using CommunityToolkit.Maui.Core;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class MultiplePopupViewModel(IPopupService popupService) : BaseViewModel
{
	[RelayCommand]
	Task OnCsharpBindingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<CsharpBindingPopupViewModel>(
			onPresenting: viewModel => viewModel.Load("This is a platform specific popup with a .NET MAUI View being rendered. The behaviors of the popup will confirm to 100% this platform look and feel, but still allows you to use your .NET MAUI Controls."),
			token);
	}

	[RelayCommand]
	Task OnUpdatingPopup(CancellationToken token)
	{
		return popupService.ShowPopupAsync<UpdatingPopupViewModel>(
			onPresenting: viewModel => viewModel.PerformUpdates(10),
			token);
	}
}