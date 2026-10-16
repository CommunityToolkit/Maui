using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Behaviors;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class NumericValidationBehaviorPage : BasePage<NumericValidationBehaviorViewModel>
{
	public NumericValidationBehaviorPage(NumericValidationBehaviorViewModel numericValidationBehaviorViewModel)
		: base(numericValidationBehaviorViewModel)
	{
		InitializeComponent();
	}

	async void SetSafeEntryValue(object? sender, EventArgs e)
	{
		// NumericValidationBehavior does not throw for null: it validates null as invalid
		SafeEntry.Text = null;

		// Keep the text short: Android 12+ shows at most two lines in a Toast
		await Toast.Make($"Text set to null: invalid for {nameof(NumericValidationBehavior)}", Core.ToastDuration.Long).Show(CancellationToken.None);
	}
}