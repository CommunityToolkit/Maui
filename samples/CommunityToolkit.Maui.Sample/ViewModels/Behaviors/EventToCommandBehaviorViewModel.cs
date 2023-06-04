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
		IncrementCommand2 = new Command<EventArgs>((args) => ClickCount++);
	}

	public ICommand IncrementCommand { get; }
	public ICommand IncrementCommand2 { get; }
}