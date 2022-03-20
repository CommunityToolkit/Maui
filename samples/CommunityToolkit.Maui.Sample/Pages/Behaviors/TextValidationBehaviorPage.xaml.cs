using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class TextValidationBehaviorPage : BasePage<TextValidationBehaviorViewModel>
{
	public TextValidationBehaviorPage(IDeviceInfo deviceInfo, TextValidationBehaviorViewModel textValidationBehaviorViewModel)
		: base(deviceInfo, textValidationBehaviorViewModel)
	{
		InitializeComponent();
	}
}