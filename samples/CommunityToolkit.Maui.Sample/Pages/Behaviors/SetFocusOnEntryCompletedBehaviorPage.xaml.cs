using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class SetFocusOnEntryCompletedBehaviorPage : BasePage<SetFocusOnEntryCompletedBehaviorViewModel>
{
	public SetFocusOnEntryCompletedBehaviorPage(IDeviceInfo deviceInfo, SetFocusOnEntryCompletedBehaviorViewModel setFocusOnEntryCompletedBehaviorViewModel)
		: base(deviceInfo, setFocusOnEntryCompletedBehaviorViewModel)
	{
		InitializeComponent();
	}
}