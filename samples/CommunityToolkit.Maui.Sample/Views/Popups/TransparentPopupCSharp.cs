using CommunityToolkit.Maui.Sample.Models;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample;

public class TransparentPopupCSharp : Popup
{
	public TransparentPopupCSharp()
	{
		Size = PopupSizeConstants.Small; 

		Content = new Frame
		{
			CornerRadius = 25,
			HeightRequest = 50,
			WidthRequest = 50
		};
	}
}
