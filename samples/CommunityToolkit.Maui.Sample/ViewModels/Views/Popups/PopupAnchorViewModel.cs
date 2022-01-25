using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.Pages.Views.Popups;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views.Popups;
public sealed class PopupAnchorViewModel
{
	public PopupAnchorViewModel()
	{
		ShowPopup = new Command<View>(OnShowPopup);
	}

	INavigation? Navigation => Application.Current?.MainPage?.Navigation;

	public ICommand ShowPopup { get; }

	void OnShowPopup(View anchor)
	{
		// This works

		//var popup = new TransparentPopupCSharp
		//{
		//	Anchor = anchor
		//};

		// This doesn't work

		var popup = new TransparentPopup
		{
			Anchor = anchor
		};

		Navigation?.ShowPopup(popup);
	}
}
