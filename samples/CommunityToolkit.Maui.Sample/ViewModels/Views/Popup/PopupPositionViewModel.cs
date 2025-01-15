using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;
using LayoutAlignment = Microsoft.Maui.Primitives.LayoutAlignment;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class PopupPositionViewModel : BaseViewModel
{
	static Page Page => Application.Current?.Windows[0].Page ?? throw new InvalidOperationException("MainPage cannot be null");

	[RelayCommand]
	static async Task DisplayPopup(PopupPosition position)
	{
		var popup = new TransparentPopup();

		await Page.Navigation.ShowPopup(popup, new PopupOptions<TransparentPopup>());
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