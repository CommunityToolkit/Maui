using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewDayOfWeekPage : BasePage<AvatarViewDayOfWeekViewModel>
{
	public AvatarViewDayOfWeekPage(AvatarViewDayOfWeekViewModel avatarViewDayOfWeekViewModel) : base(avatarViewDayOfWeekViewModel)
	{
		InitializeComponent();
	}
}