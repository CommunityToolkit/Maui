namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView sizes.</summary>
public partial class SizesAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="SizesAvatarView"/> class.</summary>
	public SizesAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}