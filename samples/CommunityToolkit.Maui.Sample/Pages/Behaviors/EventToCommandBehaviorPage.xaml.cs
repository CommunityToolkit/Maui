using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EventToCommandBehaviorPage : BasePage<EventToCommandBehaviorViewModel>
{
	public EventToCommandBehaviorPage(EventToCommandBehaviorViewModel eventToCommandBehaviorViewModel)
		: base(eventToCommandBehaviorViewModel)
	{
		InitializeComponent();
	}
}