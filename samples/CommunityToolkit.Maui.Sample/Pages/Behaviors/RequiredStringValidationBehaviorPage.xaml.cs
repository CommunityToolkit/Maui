using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class RequiredStringValidationBehaviorPage : BasePage<RequiredStringValidationBehaviorViewModel>
{
	public RequiredStringValidationBehaviorPage(IDeviceInfo deviceInfo, RequiredStringValidationBehaviorViewModel requiredStringValidationBehaviorViewModel)
		: base(deviceInfo, requiredStringValidationBehaviorViewModel)
	{
		InitializeComponent();
	}
}