using CommunityToolkit.Maui.Sample.ViewModels.Essentials;

namespace CommunityToolkit.Maui.Sample.Pages.Essentials;

public partial class BadgePage : BasePage<BadgeViewModel>
{
	public BadgePage(BadgeViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
	}
}