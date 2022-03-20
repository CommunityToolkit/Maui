using System.Windows.Input;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public sealed class PopupAnchorViewModel : BaseViewModel
{
	public PopupAnchorViewModel()
	{
		ShowPopup = new Command<View>(OnShowPopup);
	}

	public ICommand ShowPopup { get; }

	static Page Page => Application.Current?.MainPage ?? throw new NullReferenceException();

	void OnShowPopup(View anchor)
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