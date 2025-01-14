using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ReturnResultPopup : Maui.Views.Popup
{
	public ReturnResultPopup()
	{
		InitializeComponent();
	}

	void Button_Clicked(object? sender, EventArgs e)
	{
		Close("Close button tapped");
	}
}