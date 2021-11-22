using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class EventToCommandBehaviorViewModel : BaseViewModel
{
	int _clickCount;

	public EventToCommandBehaviorViewModel()
	{
		IncrementCommand = new Command(() => ClickCount++);
	}

	public int ClickCount
	{
		get => _clickCount;
		set => SetProperty(ref _clickCount, value);
	}

	public ICommand IncrementCommand { get; }
}