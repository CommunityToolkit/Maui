namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample keyboard ideas for AvatarView.</summary>
public partial class SamplesKeyboardAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="SamplesKeyboardAvatarView"/> class.</summary>
	public SamplesKeyboardAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}