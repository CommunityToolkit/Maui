using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class TransparentPopup : Popup
{
	public TransparentPopup() => InitializeComponent();

	public async void CloseButtonClicked(object? sender, EventArgs args)
	{
		await CloseAsync();
	}
}