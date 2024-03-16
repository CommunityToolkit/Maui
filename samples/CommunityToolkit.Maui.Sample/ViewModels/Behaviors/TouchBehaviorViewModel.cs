using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class TouchBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	bool isNativeAnimationBorderless;

	[ObservableProperty]
	int touchCount, longPressCount;

	static Task DisplayAlert(string title, CancellationToken token)
	{
		return Shell.Current.DisplayAlert(title, null, "Ok").WaitAsync(token);
	}

	[RelayCommand]
	async Task ParentClicked(CancellationToken token)
	{
		await DisplayAlert("Parent Clicked", token);
	}

	[RelayCommand]
	async Task ChildClicked(CancellationToken token)
	{
		await DisplayAlert("Child Clicked", token);
	}

	[RelayCommand]
	void IncreaseTouchCount()
	{
		TouchCount++;
	}

	[RelayCommand]
	void IncreaseLongPressCount()
	{
		LongPressCount++;
	}
}