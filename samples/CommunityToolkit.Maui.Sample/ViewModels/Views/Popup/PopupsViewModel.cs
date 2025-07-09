using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupsViewModel(IPopupService popupService) : BaseViewModel
{
	static INavigation currentNavigation => Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException($"{nameof(Page.Navigation)} not found");

	[ObservableProperty]
	public partial Color PageOverlayBackgroundColor { get; set; } = Colors.Orange.WithAlpha(0.2f);

	[RelayCommand]
	void OnCsharpBindingPopup()
	{
		var queryAttributes = new Dictionary<string, object>
		{
			[nameof(CsharpBindingPopupViewModel.Title)] = "C# Binding Popup",
			[nameof(CsharpBindingPopupViewModel.Message)] = "This message uses a ViewModel binding that was set using IQueryAttributable"
		};
		popupService.ShowPopup<CsharpBindingPopupViewModel>(Shell.Current, null, queryAttributes);
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

	[RelayCommand]
	async Task OnComplexPopupOpened(CancellationToken token)
	{
		// Rotate the PopupOptions.PageOverlayBackgroundColor every second
		while (!token.IsCancellationRequested)
		{
			PageOverlayBackgroundColor = Color.FromRgba(Random.Shared.NextDouble(), Random.Shared.NextDouble(), Random.Shared.NextDouble(), 0.2f);
			await Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None);
		}
	}
}