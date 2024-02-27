using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;

namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class StatusBarBehaviorPage : BasePage<StatusBarBehaviorViewModel>
{
	public StatusBarBehaviorPage(StatusBarBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
	}
}