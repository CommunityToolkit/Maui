using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public class PopupPositionViewModel
{
	public PopupPositionViewModel()
	{
		DisplayPopup = new Command<PopupPosition>(OnDisplayPopup);
	}

	INavigation Navigation => Application.Current?.MainPage?.Navigation ?? throw new NullReferenceException();

	public ICommand DisplayPopup { get; }

	void OnDisplayPopup(PopupPosition position)
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

		Navigation.ShowPopup(popup);
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