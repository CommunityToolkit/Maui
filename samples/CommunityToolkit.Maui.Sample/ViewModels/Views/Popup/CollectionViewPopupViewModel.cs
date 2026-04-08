using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class CollectionViewPopupViewModel(IPopupService popupService) : ObservableObject
{
	readonly INavigation navigation = Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException("Unable to locate INavigation");

	bool CanReturnButtonExecute => SelectedTitle?.Length > 0;

	[RelayCommand(CanExecute = nameof(CanReturnButtonExecute))]
	async Task OnReturnButtonTapped(CancellationToken token)
	{
		await popupService.ClosePopupAsync<string>(navigation, SelectedTitle ?? string.Empty, token);
	}

	[ObservableProperty, NotifyCanExecuteChangedFor(nameof(ReturnButtonTappedCommand))]
	public partial string? SelectedTitle { get; set; }
}