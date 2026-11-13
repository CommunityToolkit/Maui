using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class ComplexPopupViewModel(IPopupService popupService) : ObservableObject
{
	readonly IPopupService popupService = popupService;
	readonly INavigation navigation = Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException("Unable to locate INavigation");

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ReturnButtonTappedCommand))]
	public partial string ReturnText { get; set; } = string.Empty;

	bool CanReturnButtonExecute => ReturnText?.Length > 0;

	[RelayCommand(CanExecute = nameof(CanReturnButtonExecute))]
	async Task OnReturnButtonTapped(CancellationToken token)
	{
		await popupService.ClosePopupAsync<string>(navigation, ReturnText, token);
	}
}