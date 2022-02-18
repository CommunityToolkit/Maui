using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed class PopupAnchorViewModel : BaseViewModel
{
	public PopupAnchorViewModel()
	{
		ShowPopup = new Command<View>(OnShowPopup);
	}

	INavigation? Navigation => Application.Current?.MainPage?.Navigation;

	public ICommand ShowPopup { get; }

	void OnShowPopup(View anchor)
	{

		// Using the C# version of Popup until this get fixed
		// https://github.com/dotnet/maui/issues/4300

		// This works

		var popup = new TransparentPopupCSharp
		{
			Anchor = anchor
		};

		// This doesn't work

		//var popup = new TransparentPopup
		//{
		//	Anchor = anchor
		//};

		Navigation?.ShowPopup(popup);
	}
}
