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

#if !DEBUG
	void SetEntryValue(object? sender, EventArgs e)
	{
#else
	async void SetEntryValue(object? sender, EventArgs e)
	{
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		await Toast.Make($"The app will crash because {nameof(Options.SetShouldSuppressExceptionsInBehaviors)} is false", Core.ToastDuration.Long).Show(cts.Token);
#endif
		SafeEntry.Text = null;
	}
}