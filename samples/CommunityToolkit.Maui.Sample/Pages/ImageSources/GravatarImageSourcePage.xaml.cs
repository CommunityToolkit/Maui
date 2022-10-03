namespace CommunityToolkit.Maui.Sample.Pages.ImageSources;

using CommunityToolkit.Maui.Sample.ViewModels.ImageSources;

public partial class GravatarImageSourcePage : BasePage<GravatarImageSourceViewModel>
{
	public GravatarImageSourcePage(GravatarImageSourceViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Padding = new Thickness(Padding.Left, 0, Padding.Right, 0);
	}
}