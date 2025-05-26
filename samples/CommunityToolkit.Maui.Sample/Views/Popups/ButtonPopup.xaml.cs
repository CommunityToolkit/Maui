namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ButtonPopup : Maui.Views.Popup
{
	public ButtonPopup()
	{
		InitializeComponent();
	}

	void Button_Clicked(object? sender, EventArgs e)
	{
		CloseAsync();
	}
}