﻿using CommunityToolkit.Maui.Core;
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
		var verticalOptions = LayoutOptions.Start;
		var horizontalOptions = LayoutOptions.Start;
		switch (position)
		{
			case PopupPosition.TopLeft:
				verticalOptions = LayoutOptions.Start;
				horizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Top:
				verticalOptions = LayoutOptions.Start;
				horizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.TopRight:
				verticalOptions = LayoutOptions.Start;
				horizontalOptions = LayoutOptions.End;
				break;
			case PopupPosition.Left:
				verticalOptions = LayoutOptions.Center;
				horizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Center:
				verticalOptions = LayoutOptions.Center;
				horizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.Right:
				verticalOptions = LayoutOptions.Center;
				horizontalOptions = LayoutOptions.End;
				break;
			case PopupPosition.BottomLeft:
				verticalOptions = LayoutOptions.End;
				horizontalOptions = LayoutOptions.Start;
				break;
			case PopupPosition.Bottom:
				verticalOptions = LayoutOptions.End;
				horizontalOptions = LayoutOptions.Center;
				break;
			case PopupPosition.BottomRight:
				verticalOptions = LayoutOptions.End;
				horizontalOptions = LayoutOptions.End;
				break;
		}

		await Page.Navigation.ShowPopup<TransparentPopup>(new TransparentPopup(), new PopupOptions()
		{
			VerticalOptions = verticalOptions,
			HorizontalOptions = horizontalOptions
		});
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