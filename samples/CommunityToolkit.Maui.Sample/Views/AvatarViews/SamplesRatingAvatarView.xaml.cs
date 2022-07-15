namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample rating ideas for AvatarView.</summary>
public partial class SamplesRatingAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="SamplesRatingAvatarView"/> class.</summary>
	public SamplesRatingAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}