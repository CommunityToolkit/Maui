using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class EventToCommandBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	int clickCount;

	[RelayCommand]
	void Increment()
	{
		ClickCount++;
	}

	[RelayCommand]
	void IncrementWithArgs(EventArgs eventArgs)
	{
		ClickCount++;
	}
}