using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class EventToCommandBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	public partial int ClickCount { get; private set; }

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