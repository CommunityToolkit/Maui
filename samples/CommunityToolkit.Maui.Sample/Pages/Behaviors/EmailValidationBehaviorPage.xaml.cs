using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class EmailValidationBehaviorPage : BasePage
{
	public EmailValidationBehaviorPage()
	{
		InitializeComponent();
		EmailValidator ??= new EmailValidationBehavior();
	}
}