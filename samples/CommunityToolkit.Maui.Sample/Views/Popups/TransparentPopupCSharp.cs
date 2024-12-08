using CommunityToolkit.Maui.Markup;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public sealed partial class TransparentPopupCSharp : Popup
{
	public TransparentPopupCSharp(Size popupSize) : this()
	{
		Size = popupSize;
		Color = Colors.Transparent;
	}

	public TransparentPopupCSharp()
	{
		Content = new Border
		{
			StrokeShape = new RoundRectangle
			{
				CornerRadius = 25
			}
		}
			.Size(50, 50)
			.Margin(10);
	}
}