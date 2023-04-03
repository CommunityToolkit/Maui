using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class BadgeCounterPage : BasePage<BadgeCounterViewModel>
{
	public BadgeCounterPage(BadgeCounterViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}