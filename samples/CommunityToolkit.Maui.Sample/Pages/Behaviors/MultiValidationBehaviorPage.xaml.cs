using CommunityToolkit.Maui.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MultiValidationBehaviorPage
{
	public MultiValidationBehaviorPage()
	{
		InitializeComponent();
		MultiValidation ??= new MultiValidationBehavior();
		digit ??= new CharactersValidationBehavior();
		upper ??= new CharactersValidationBehavior();
		symbol ??= new CharactersValidationBehavior();
		any ??= new CharactersValidationBehavior();
	}
}