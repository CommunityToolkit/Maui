using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class NumericValidationBehaviorPage : BasePage<NumericValidationBehaviorViewModel>
{
	public NumericValidationBehaviorPage(NumericValidationBehaviorViewModel numericValidationBehaviorViewModel)
		: base(numericValidationBehaviorViewModel)
	{
		InitializeComponent();
	}

	async void SetEntryValue(object? sender, EventArgs e)
	{
#if DEBUG
		await Toast.Make($"The app will crash because {nameof(Options.SetShouldSuppressExceptionsInBehaviors)} is false", Core.ToastDuration.Long).Show();
#endif
		SafeEntry.Text = null;
	}
}