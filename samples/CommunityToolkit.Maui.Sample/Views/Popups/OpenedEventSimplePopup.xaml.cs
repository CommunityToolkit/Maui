using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class OpenedEventSimplePopup
{
	public OpenedEventSimplePopup()
	{
		InitializeComponent();
		OnOpened += async (s,e) =>
		{
			await Task.Delay(TimeSpan.FromSeconds(1));

			Title.Text = "Opened Event Popup";
			Message.Text = "The content of this popup was updated after the popup was rendered";
		};
	}
}