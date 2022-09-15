using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class StatusBarBehaviorPage : BasePage<StatusBarBehaviorViewModel>
{
	public StatusBarBehaviorPage(StatusBarBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		var statusBarColor = Color.FromRgb(BindingContext.RedSliderValue, BindingContext.GreenSliderValue, BindingContext.BlueSliderValue);
		CommunityToolkit.Maui.Core.Platform.StatusBar.SetColor(statusBarColor);
	}
}