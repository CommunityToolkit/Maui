using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public partial class EventToCommandBehaviorViewModel : BaseViewModel
{
	[ObservableProperty]
	int clickCount;

	public EventToCommandBehaviorViewModel()
	{
		IncrementCommand = new Command(() => ClickCount++);
	}

	public ICommand IncrementCommand { get; }
}