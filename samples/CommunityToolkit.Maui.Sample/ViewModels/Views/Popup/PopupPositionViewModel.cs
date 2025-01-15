using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupPositionViewModel : BaseViewModel
{
	static Page Page => Application.Current?.Windows[0].Page ?? throw new InvalidOperationException("MainPage cannot be null");

	[RelayCommand]
	static async Task DisplayPopup(PopupPosition position)
	{
		var popupOptions = new PopupOptions();
		switch (position)
		{
			case PopupPosition.TopLeft:
				popupOptions.VerticalOptions = LayoutOptions.Start;
				popupOptions.HorizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Top:
				popupOptions.VerticalOptions = LayoutOptions.Start;
				popupOptions.HorizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.TopRight:
				popupOptions.VerticalOptions = LayoutOptions.Start;
				popupOptions.HorizontalOptions = LayoutOptions.End;
				break;
			case PopupPosition.Left:
				popupOptions.VerticalOptions = LayoutOptions.Center;
				popupOptions.HorizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Center:
				popupOptions.VerticalOptions = LayoutOptions.Center;
				popupOptions.HorizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.Right:
				popupOptions.VerticalOptions = LayoutOptions.Center;
				popupOptions.HorizontalOptions = LayoutOptions.End;
				break;
			case PopupPosition.BottomLeft:
				popupOptions.VerticalOptions = LayoutOptions.End;
				popupOptions.HorizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Bottom:
				popupOptions.VerticalOptions = LayoutOptions.End;
				popupOptions.HorizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.BottomRight:
				popupOptions.VerticalOptions = LayoutOptions.End;
				popupOptions.HorizontalOptions = LayoutOptions.End;
				break;
		}

		await Page.Navigation.ShowPopup<TransparentPopup>(new TransparentPopup(), popupOptions);
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