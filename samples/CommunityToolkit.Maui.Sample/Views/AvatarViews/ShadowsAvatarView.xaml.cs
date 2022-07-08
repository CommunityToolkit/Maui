namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView shadows.</summary>
public partial class ShadowsAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="ShadowsAvatarView"/> class.</summary>
	public ShadowsAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}