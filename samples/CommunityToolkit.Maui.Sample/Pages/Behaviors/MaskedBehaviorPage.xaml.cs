using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MaskedBehaviorPage : BasePage<MaskedBehaviorViewModel>
{
	public MaskedBehaviorPage(IDeviceInfo deviceInfo, MaskedBehaviorViewModel maskedBehaviorViewModel)
		: base(deviceInfo, maskedBehaviorViewModel)
	{
		InitializeComponent();
	}
}