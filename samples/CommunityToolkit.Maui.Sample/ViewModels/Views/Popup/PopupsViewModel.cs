using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Views.Popup;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupsViewModel(IPopupService popupService) : BaseViewModel
{
	[ObservableProperty]
	public partial Color PageOverlayBackgroundColor { get; set; } = Colors.Orange.WithAlpha(0.2f);

	[ObservableProperty]
	public partial string TryPopupServiceResultText { get; set; } = "None yet";

	static INavigation currentNavigation => Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException($"{nameof(Page.Navigation)} not found");

	[RelayCommand]
	async Task OnCsharpBindingPopup()
	{
		var queryAttributes = new Dictionary<string, object>
		{
			[nameof(CsharpBindingPopupViewModel.Title)] = "C# Binding Popup",
			[nameof(CsharpBindingPopupViewModel.Message)] = "This message uses a ViewModel binding that was set using IQueryAttributable"
		};
		await popupService.ShowPopupAsync<CsharpBindingPopupViewModel>(Shell.Current, null, queryAttributes);
	}

	[RelayCommand]
	async Task OnUpdatingPopup()
	{
		await popupService.ShowPopupAsync<UpdatingPopupViewModel>(currentNavigation);
	}

	[RelayCommand]
	async Task OnTryShowPopupWithPopupService(CancellationToken token)
	{
		var resultOrException = await popupService.TryShowPopupAsync<ReturnResultPopup, string>(currentNavigation, cancellationToken: token);
		TryPopupServiceResultText = PopupResultOrExceptionDescriber.Describe(resultOrException);
	}

	[RelayCommand]
	async Task OnTryShowUnregisteredPopupWithPopupService(CancellationToken token)
	{
		var resultOrException = await popupService.TryShowPopupAsync<UnregisteredPopupView>(currentNavigation, cancellationToken: token);
		TryPopupServiceResultText = PopupResultOrExceptionDescriber.Describe(resultOrException);
	}

	[RelayCommand]
	async Task OnTryClosePopupWithPopupService(CancellationToken token)
	{
		var resultOrException = await popupService.TryClosePopupAsync(currentNavigation, cancellationToken: token);
		TryPopupServiceResultText = PopupResultOrExceptionDescriber.Describe(resultOrException);
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

	sealed class UnregisteredPopupView : ContentView;
}