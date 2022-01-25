using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views.Popups;
public class TransparentPopupCSharp : Popup
{
	public TransparentPopupCSharp()
	{
		Size = PopupSize.Small;
		var frame = new Frame
		{
			CornerRadius = 25,
			HeightRequest = 50,
			WidthRequest = 50
		};

		Content = frame;
	}
}
