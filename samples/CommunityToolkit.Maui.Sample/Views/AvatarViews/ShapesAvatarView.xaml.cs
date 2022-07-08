namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView shapes.</summary>
public partial class ShapesAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="ShapesAvatarView"/> class.</summary>
	public ShapesAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}