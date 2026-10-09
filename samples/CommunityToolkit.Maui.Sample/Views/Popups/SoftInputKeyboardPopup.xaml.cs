using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class SoftInputKeyboardPopup : Popup
{
	public SoftInputKeyboardPopup()
	{
		InitializeComponent();

		#if IOS
		PickerForIOSLabel.IsVisible = true;
		PickerForIOS.IsVisible = true;
		DatePickerForIOSLabel.IsVisible = true;
		DatePickerForIOS.IsVisible = true;
		#endif
	}

	async void ClosePopup(object? sender, EventArgs eventArgs)
	{
		await CloseAsync();
	}
}