using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class ItemViewModel : BaseViewModel
{
	public string Title { get; }
	
	public ItemViewModel(string title)
	{
		Title = title;
	}

	[RelayCommand]
	public Task TapAsync()
	{
		return Application.Current?.MainPage?.DisplayAlert("Tap", Title, "OK") ?? Task.CompletedTask;
	}
}