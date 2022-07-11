namespace CommunityToolkit.Maui.Sample;

using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

/// <summary>View for sample AvatarView using bindable layouts.</summary>
public partial class BindableLayoutsAvatarView : Popup
{
	/// <summary>Initialises a new instance of the <see cref="BindableLayoutsAvatarView"/> class.</summary>
	public BindableLayoutsAvatarView(PopupSizeConstants popupSizeConstants, AvatarViewBindingPopupViewModel avatarViewBindingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = avatarViewBindingPopupViewModel;
		Size = popupSizeConstants.Medium;
	}
}