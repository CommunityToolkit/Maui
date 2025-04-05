namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class OpenedEventSimplePopup
{
	public OpenedEventSimplePopup()
	{
		InitializeComponent();
		Opened += async (s, e) =>
		{
			await Task.Delay(TimeSpan.FromSeconds(1));

			Title.Text = "Opened Event Popup";
			Message.Text = "The content of this popup was updated after the popup was rendered";
		};
	}
}