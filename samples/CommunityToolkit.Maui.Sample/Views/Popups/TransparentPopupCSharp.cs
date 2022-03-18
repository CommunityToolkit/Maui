using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public class TransparentPopupCSharp : Popup
{
	public TransparentPopupCSharp(Size popupSize)
		: this()
	{
		Size = popupSize;
		Color = Colors.Transparent;
	}

	public TransparentPopupCSharp()
	{
		Content = new Frame
		{
			CornerRadius = 25,
			HeightRequest = 50,
			WidthRequest = 50
		};
	}
}