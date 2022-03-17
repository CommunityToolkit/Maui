using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ButtonPopup : Popup
{
	public ButtonPopup()
	{
		InitializeComponent();
	}

	void Button_Clicked(object? sender, System.EventArgs e) => Close();
}