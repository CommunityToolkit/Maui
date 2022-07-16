using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views.AvatarView;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class AvatarViewSampleRatingPage : BasePage<AvatarViewSampleRatingViewModel>
{
	public AvatarViewSampleRatingPage(AvatarViewSampleRatingViewModel avatarViewSampleRatingViewModel) : base(avatarViewSampleRatingViewModel)
	{
		InitializeComponent();
	}
}