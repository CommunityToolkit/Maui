using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EventToCommandBehaviorPage : BasePage<EventToCommandBehaviorViewModel>
{
	public EventToCommandBehaviorPage(IDeviceInfo deviceInfo, EventToCommandBehaviorViewModel eventToCommandBehaviorViewModel)
		: base(deviceInfo, eventToCommandBehaviorViewModel)
	{
		InitializeComponent();
	}
}