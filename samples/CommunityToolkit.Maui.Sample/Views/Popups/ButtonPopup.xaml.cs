using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ButtonPopup : Popup
{
	public ButtonPopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();
		Size = popupSizeConstants.Medium;
	}

	void Button_Clicked(object? sender, EventArgs e) => Close();
}