using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public sealed class TransparentPopupCSharp : Popup
{
	public TransparentPopupCSharp(Size popupSize) : this()
	{
		Size = popupSize;
		Color = Colors.Transparent;
	}

	public TransparentPopupCSharp()
	{
		Content = new Frame { CornerRadius = 25 }
					.Size(50, 50)
					.Margin(10);
	}
}