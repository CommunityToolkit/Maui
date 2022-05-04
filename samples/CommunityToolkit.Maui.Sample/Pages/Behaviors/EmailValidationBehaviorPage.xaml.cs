using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EmailValidationBehaviorPage : BasePage<EmailValidationBehaviorViewModel>
{
	public EmailValidationBehaviorPage(EmailValidationBehaviorViewModel emailValidationBehaviorViewModel)
		: base(emailValidationBehaviorViewModel)
	{
		InitializeComponent();
		EmailValidator ??= new();
	}
}