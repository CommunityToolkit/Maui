namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView colours.</summary>
public partial class ColoursAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="ColoursAvatarView"/> class.</summary>
	public ColoursAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}