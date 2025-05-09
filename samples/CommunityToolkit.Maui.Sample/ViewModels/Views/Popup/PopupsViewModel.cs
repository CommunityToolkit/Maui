using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupsViewModel(IPopupService popupService) : BaseViewModel
{
	static INavigation currentNavigation => Application.Current?.Windows[0].Page?.Navigation ?? throw new InvalidOperationException($"{nameof(Page.Navigation)} not found");

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
}