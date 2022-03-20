using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class MultipleButtonPopup : Popup
{
	public MultipleButtonPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
	}

	void Cancel_Clicked(object? sender, EventArgs e) => Close(false);

	void Okay_Clicked(object? sender, EventArgs e) => Close(true);
}