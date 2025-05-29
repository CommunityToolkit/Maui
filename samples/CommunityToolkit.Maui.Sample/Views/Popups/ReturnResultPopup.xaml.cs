namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ReturnResultPopup
{
	public ReturnResultPopup()
	{
		InitializeComponent();
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync("Close button tapped");
	}
}