namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class ToggleSizePopup : Maui.Views.Popup
{
	readonly Size originalSize;

	public ToggleSizePopup()
	{
		InitializeComponent();
		originalSize = new Size(300, 300);
		WidthRequest = 300;
		HeightRequest = 300;
	}

	void Button_Clicked(object? sender, EventArgs e)
	{
		if (originalSize == new Size(WidthRequest, HeightRequest))
		{
			WidthRequest = originalSize.Width * 1.25;
			HeightRequest = originalSize.Height * 1.25;
		}
		else
		{
			WidthRequest = originalSize.Width;
			HeightRequest = originalSize.Height;
		}
	}
}