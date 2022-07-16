using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.Pages;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewBordersPage : BasePage<AvatarViewBordersViewModel>
{
	public AvatarViewBordersPage(AvatarViewBordersViewModel bordersAvatarViewViewModel) : base(bordersAvatarViewViewModel)
	{
		InitializeComponent();
	}
}