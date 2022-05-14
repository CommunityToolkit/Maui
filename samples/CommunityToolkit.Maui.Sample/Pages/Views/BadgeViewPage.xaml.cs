using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class BadgeViewPage : BasePage<BadgeViewViewModel>
{
	public BadgeViewPage(BadgeViewViewModel badgeViewViewModel)
		: base(badgeViewViewModel)
	{
		InitializeComponent();
	}
}