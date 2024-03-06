using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class TouchBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	bool nativeAnimationBorderless;

	[ObservableProperty]
	int touchCount;

	[ObservableProperty]
	int longPressCount;

	[RelayCommand]
	void ParentClicked()
	{
		DisplayAlert("Parent Clicked");
	}

	[RelayCommand]
	void ChildClicked()
	{
		DisplayAlert("Child Clicked");
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

	void DisplayAlert(string title)
	{
		Shell.Current.DisplayAlert(title, null, "Ok");
	}
}