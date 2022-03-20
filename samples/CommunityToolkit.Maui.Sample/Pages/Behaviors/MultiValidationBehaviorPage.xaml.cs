using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class MultiValidationBehaviorPage : BasePage<MultiValidationBehaviorViewModel>
{
	public MultiValidationBehaviorPage(IDeviceInfo deviceInfo, MultiValidationBehaviorViewModel multiValidationBehaviorViewModel)
		: base(deviceInfo, multiValidationBehaviorViewModel)
	{
		InitializeComponent();

		AnyValidation ??= new();
		MultiValidation ??= new();
		DigitValidation ??= new();
		UpperValidation ??= new();
		SymbolValidation ??= new();
	}
}