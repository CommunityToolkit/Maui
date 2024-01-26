using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class SystemNavigationBarBehaviorPage : BasePage<SystemNavigationBarBehaviorViewModel>
{
	public SystemNavigationBarBehaviorPage(SystemNavigationBarBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
	}

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		var systemNavigationBarColor = Color.FromRgb(BindingContext.RedSliderValue, BindingContext.GreenSliderValue, BindingContext.BlueSliderValue);
		CommunityToolkit.Maui.Core.Platform.SystemNavigationBar.SetColor(systemNavigationBarColor);
	}
}