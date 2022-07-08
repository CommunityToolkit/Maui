using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

/// <summary>AvatarView sample page view.</summary>
public partial class AvatarViewPage : BasePage<AvatarViewViewModel>
{
	readonly PopupSizeConstants popupSizeConstants;

	/// <summary>Initialises a new instance of the <see cref="AvatarViewPage"/> class.</summary>
	public AvatarViewPage(PopupSizeConstants popupSizeConstants, AvatarViewViewModel avatarControlViewModel) : base(avatarControlViewModel)
	{
		InitializeComponent();
		this.popupSizeConstants = popupSizeConstants;
	}

	async void HandleBindableLayoutsAvatarViewButtonClicked(object sender, EventArgs e)
	{
		BindableLayoutsAvatarView bindableLayoutsView = new(popupSizeConstants);
		await this.ShowPopupAsync(bindableLayoutsView);
	}

	async void HandleBordersAvatarViewButtonClicked(object sender, EventArgs e)
	{
		BordersAvatarView bordersView = new(popupSizeConstants);
		await this.ShowPopupAsync(bordersView);
	}

	async void HandleColoursAvatarViewButtonClicked(object sender, EventArgs e)
	{
		ColoursAvatarView coloursView = new(popupSizeConstants);
		await this.ShowPopupAsync(coloursView);
	}

	async void HandleGesturesAvatarViewButtonClicked(object sender, EventArgs e)
	{
		GesturesAvatarView gesturesView = new(popupSizeConstants);
		await this.ShowPopupAsync(gesturesView);
	}

	async void HandleImagesAvatarViewButtonClicked(object sender, EventArgs e)
	{
		ImagesAvatarView imagesView = new(popupSizeConstants);
		await this.ShowPopupAsync(imagesView);
	}

	async void HandleSamplesAvatarViewButtonClicked(object sender, EventArgs e)
	{
		SamplesAvatarView samplesView = new(popupSizeConstants);
		await this.ShowPopupAsync(samplesView);
	}

	async void HandleShadowsAvatarViewButtonClicked(object sender, EventArgs e)
	{
		ShadowsAvatarView shadowsView = new(popupSizeConstants);
		await this.ShowPopupAsync(shadowsView);
	}

	async void HandleShapesAvatarViewButtonClicked(object sender, EventArgs e)
	{
		ShapesAvatarView shapesView = new(popupSizeConstants);
		await this.ShowPopupAsync(shapesView);
	}

	async void HandleSizesAvatarViewButtonClicked(object sender, EventArgs e)
	{
		SizesAvatarView sizesView = new(popupSizeConstants);
		await this.ShowPopupAsync(sizesView);
	}
}