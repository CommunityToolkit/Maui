using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class NumericValidationBehaviorPage : BasePage<NumericValidationBehaviorViewModel>
{
	public NumericValidationBehaviorPage(NumericValidationBehaviorViewModel numericValidationBehaviorViewModel)
		: base(numericValidationBehaviorViewModel)
	{
		InitializeComponent();
	}
}