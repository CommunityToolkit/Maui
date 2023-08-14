using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class OpenedEventSimplePopup : Popup
{
	public OpenedEventSimplePopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
		Opened += OnOpened;
	}

	async void OnOpened(object? sender, PopupOpenedEventArgs e)
	{
		Opened -= OnOpened;

		await Task.Delay(TimeSpan.FromSeconds(1));

		Title.Text = "Opened Event Popup";
		Message.Text = "The content of this popup was updated after the popup was rendered";
	}
}