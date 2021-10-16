using System.Windows.Input;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

public class EventToCommandBehaviorViewModel : BaseViewModel
{
    int clickCount;

    public EventToCommandBehaviorViewModel()
    {
        IncrementCommand = new Command(() => ClickCount++);
    }

    public int ClickCount
    {
        get => clickCount;
        set => SetProperty(ref clickCount, value);
    }

    public ICommand IncrementCommand { get; }
}