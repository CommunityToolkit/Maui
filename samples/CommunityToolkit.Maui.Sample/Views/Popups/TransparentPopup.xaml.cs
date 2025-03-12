namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class TransparentPopup : Maui.Views.Popup
{
	public TransparentPopup() => InitializeComponent();

	void CloseButtonClicked(object? sender, EventArgs args)
	{
		Close();
	}
}