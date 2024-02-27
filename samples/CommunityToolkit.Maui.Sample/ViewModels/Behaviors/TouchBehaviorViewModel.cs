using System.Diagnostics;
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
		// DisplayAlert("Parent Clicked", null, "Ok");
		Debug.WriteLine("Parent Clicked");
	}

	[RelayCommand]
	void ChildClicked()
	{
		// DisplayAlert("Child Clicked", null, "Ok");
		Debug.WriteLine("Child Clicked");
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