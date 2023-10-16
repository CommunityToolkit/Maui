using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class MultipleButtonPopup : Popup
{
	public MultipleButtonPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
	}

	async void Cancel_Clicked(object? sender, EventArgs e) => await CloseAsync(false);

	async void Okay_Clicked(object? sender, EventArgs e) => await CloseAsync(true);
}