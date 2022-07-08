namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample ideas for AvatarView.</summary>
public partial class SamplesAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="SamplesAvatarView"/> class.</summary>
	public SamplesAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}