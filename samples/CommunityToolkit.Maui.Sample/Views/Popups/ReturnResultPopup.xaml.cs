using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ReturnResultPopup : Popup
{
	public ReturnResultPopup()
	{
		InitializeComponent();
		ResultWhenUserTapsOutsideOfPopup = "User Tapped Outside of Popup";
	}

	void Button_Clicked(object? sender, EventArgs e) => Close("Close button tapped");
}