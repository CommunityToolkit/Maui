using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class OpenedEventSimplePopup
{
	public OpenedEventSimplePopup()
	{
		InitializeComponent();
		Opened += OnOpened;
		Title ??= new();
		Message ??= new();

	}

	void OnOpened(object? sender, PopupOpenedEventArgs e)
	{
		Opened -= OnOpened;

		Title.Text = "Opened Event Popup";
		Message.Text = "The content of this popup was updated after the popup was rendered";
	}
}