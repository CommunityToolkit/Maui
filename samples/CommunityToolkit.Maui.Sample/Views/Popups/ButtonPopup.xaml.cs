namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ButtonPopup : Maui.Views.Popup
{
	public ButtonPopup()
	{
		InitializeComponent();
	}

	async void Button_Clicked(object? sender, EventArgs e)
	{
		await CloseAsync();
	}

	void Label_Loaded(object? sender, EventArgs e)
	{
		if (sender is Label label)
		{
			label.SetSemanticFocus();
		}
	}
}