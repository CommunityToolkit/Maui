using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class TransparentPopup : Popup
{
	public TransparentPopup() => InitializeComponent();

	public void CloseButtonClicked(object? sender, EventArgs args)
	{
		Close();
	}
}