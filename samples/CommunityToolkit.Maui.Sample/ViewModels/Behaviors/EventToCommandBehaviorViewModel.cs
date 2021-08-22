using Microsoft.Maui.Controls;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Behaviors
{
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
}