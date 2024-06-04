using CommunityToolkit.Maui.Sample.ViewModels.Behaviors;
namespace CommunityToolkit.Maui.Sample.Pages.Behaviors;

public partial class TouchBehaviorPage : BasePage<TouchBehaviorViewModel>
{
	public TouchBehaviorPage(TouchBehaviorViewModel viewModel)
		: base(viewModel)
	{
		InitializeComponent();
	}
}