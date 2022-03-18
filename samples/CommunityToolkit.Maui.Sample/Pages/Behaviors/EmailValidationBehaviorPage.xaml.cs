using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EmailValidationBehaviorPage : BasePage<EmailValidationBehaviorViewModel>
{
	public EmailValidationBehaviorPage(IDeviceInfo deviceInfo, EmailValidationBehaviorViewModel emailValidationBehaviorViewModel)
		: base(deviceInfo, emailValidationBehaviorViewModel)
	{
		InitializeComponent();
		EmailValidator ??= new();
	}
}