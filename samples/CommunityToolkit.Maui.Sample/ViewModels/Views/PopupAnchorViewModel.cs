using System.Windows.Input;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed partial class PopupAnchorViewModel : BaseViewModel
{
	static Page Page => Application.Current?.MainPage ?? throw new NullReferenceException();

	[RelayCommand]
	static void ShowPopup(View anchor)
	{

		// Using the C# version of Popup until this get fixed
		// https://github.com/dotnet/maui/issues/4300

		// This works

		var popup = new TransparentPopupCSharp()
		{
			Anchor = anchor
		};

		// This doesn't work

		//var popup = new TransparentPopup
		//{
		//	Anchor = anchor
		//};

		Page.ShowPopup(popup);
	}
}