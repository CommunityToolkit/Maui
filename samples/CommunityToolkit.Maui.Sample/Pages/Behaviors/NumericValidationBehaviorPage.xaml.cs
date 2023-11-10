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

	async void SetEntryValue(object? sender, EventArgs e)
	{
		var toastVisibilityTimeSpan = TimeSpan.FromSeconds(5);
		var cts = new CancellationTokenSource(toastVisibilityTimeSpan);

		await Toast.Make($"The app will crash because `null` is an invalid value for {nameof(NumericValidationBehavior)}.\nOptionally, {nameof(Options)}.{nameof(Options.SetShouldSuppressExceptionsInBehaviors)} can be set to true", Core.ToastDuration.Long).Show(cts.Token);

		await Task.Delay(toastVisibilityTimeSpan, cts.Token);

		SafeEntry.Text = null;
	}
}