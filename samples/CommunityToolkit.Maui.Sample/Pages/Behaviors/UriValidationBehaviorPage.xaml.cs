using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class UriValidationBehaviorPage : BasePage<UriValidationBehaviorViewModel>
{
	public UriValidationBehaviorPage(IDeviceInfo deviceInfo, UriValidationBehaviorViewModel uriValidationBehaviorViewModel)
		: base(deviceInfo, uriValidationBehaviorViewModel)
	{
		InitializeComponent();
	}
}