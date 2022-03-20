using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public partial class ToggleSizePopup : Popup
{
	readonly Size originalSize;

	public ToggleSizePopup(PopupSizeConstants popupSizeConstants)
	{
		InitializeComponent();

		Size = originalSize = popupSizeConstants.Medium;
	}

	void Button_Clicked(object? sender, EventArgs e)
	{
		if (originalSize == Size)
		{
			Size = new Size(originalSize.Width * 1.25, originalSize.Height * 1.25);
		}
		else
		{
			Size = originalSize;
		}
	}
}