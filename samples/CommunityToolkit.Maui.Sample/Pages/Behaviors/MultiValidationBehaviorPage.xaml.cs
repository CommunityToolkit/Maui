namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MultiValidationBehaviorPage
{
	public MultiValidationBehaviorPage()
	{
		InitializeComponent();

		MultiValidation ??= new();
		DigitValidation ??= new();
		UpperValidation ??= new();
		SymbolValidation ??= new();
		AnyValidation ??= new();
	}
}