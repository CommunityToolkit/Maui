using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupPositionViewModel : BaseViewModel
{
	static Page Page => Application.Current?.MainPage ?? throw new NullReferenceException();

	[RelayCommand]
	static void DisplayPopup(PopupPosition position)
	{
		// Using the C# version of Popup until this get fixed
		// https://github.com/dotnet/maui/issues/4300
		var popup = new TransparentPopupCSharp();

		switch (position)
		{
			case PopupPosition.TopLeft:
				popup.VerticalOptions = LayoutAlignment.Start;
				popup.HorizontalOptions = LayoutAlignment.Start;
				break;
			case PopupPosition.Top:
				popup.VerticalOptions = LayoutAlignment.Start;
				popup.HorizontalOptions = LayoutAlignment.Center;
				break;
			case PopupPosition.TopRight:
				popup.VerticalOptions = LayoutAlignment.Start;
				popup.HorizontalOptions = LayoutAlignment.End;
				break;
			case PopupPosition.Left:
				popup.VerticalOptions = LayoutAlignment.Center;
				popup.HorizontalOptions = LayoutAlignment.Start;
				break;
			case PopupPosition.Center:
				popup.VerticalOptions = LayoutAlignment.Center;
				popup.HorizontalOptions = LayoutAlignment.Center;
				break;
			case PopupPosition.Right:
				popup.VerticalOptions = LayoutAlignment.Center;
				popup.HorizontalOptions = LayoutAlignment.End;
				break;
			case PopupPosition.BottomLeft:
				popup.VerticalOptions = LayoutAlignment.End;
				popup.HorizontalOptions = LayoutAlignment.Start;
				break;
			case PopupPosition.Bottom:
				popup.VerticalOptions = LayoutAlignment.End;
				popup.HorizontalOptions = LayoutAlignment.Center;
				break;
			case PopupPosition.BottomRight:
				popup.VerticalOptions = LayoutAlignment.End;
				popup.HorizontalOptions = LayoutAlignment.End;
				break;
		}

		Page.ShowPopup(popup);
	}

	public enum PopupPosition
	{
		TopLeft = 0,
		Top = 1,
		TopRight = 2,
		Left = 3,
		Center = 4,
		Right = 5,
		BottomLeft = 6,
		Bottom = 7,
		BottomRight = 8
	}
}