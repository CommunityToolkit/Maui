namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MultiValidationBehaviorPage
{
	public MultiValidationBehaviorPage()
	{
		InitializeComponent();

		AnyValidation ??= new();
		MultiValidation ??= new();
		DigitValidation ??= new();
		UpperValidation ??= new();
		SymbolValidation ??= new();
	}
}