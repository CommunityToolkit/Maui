namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample day-of-week ideas for AvatarView.</summary>
public partial class SamplesDOWAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="SamplesDOWAvatarView"/> class.</summary>
	public SamplesDOWAvatarView(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}
}