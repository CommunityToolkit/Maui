namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView images.</summary>
public partial class ImagesAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="ImagesAvatarView"/> class.</summary>
	public ImagesAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}