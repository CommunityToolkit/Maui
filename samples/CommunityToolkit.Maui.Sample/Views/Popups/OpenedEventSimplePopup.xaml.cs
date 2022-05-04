using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class OpenedEventSimplePopup : Popup
{
	public OpenedEventSimplePopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = popupSizeConstants.Medium;
		Opened += OnOpened;
	}

	void OnOpened(object? sender, PopupOpenedEventArgs e)
	{
		Opened -= OnOpened;

		Title.Text = "Opened Event Popup";
		Message.Text = "The content of this popup was updated after the popup was rendered";
	}
}