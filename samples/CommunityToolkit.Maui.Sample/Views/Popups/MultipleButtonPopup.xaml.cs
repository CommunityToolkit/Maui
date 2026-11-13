namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class MultipleButtonPopup : Maui.Views.Popup<bool>
{
	public MultipleButtonPopup()
	{
		InitializeComponent();
	}

	async void Cancel_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync(false);
	}

	async void Okay_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync(true);
	}
}