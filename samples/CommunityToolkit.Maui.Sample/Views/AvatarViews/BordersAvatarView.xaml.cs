namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView borders.</summary>
public partial class BordersAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="BordersAvatarView"/> class.</summary>
	public BordersAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}