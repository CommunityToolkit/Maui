using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class PopupAnchorViewModel : BaseViewModel
{
	static Page Page => Application.Current?.MainPage ?? throw new NullReferenceException();

	[RelayCommand]
	static void ShowPopup(View anchor)
	{
		var popup = new TransparentPopupCSharp()
		{
			Anchor = anchor
		};
		Page.ShowPopup(popup);
	}
}