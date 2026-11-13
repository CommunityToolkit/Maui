using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class PopupContentView : Popup
{
	public PopupContentView(PopupContentViewModel popupContentViewModel)
	{
		InitializeComponent();

		BindingContext = popupContentViewModel;

		Opened += (s, e) =>
			popupContentViewModel.SetMessage(
				"This message was set through the view model of this Popup after it opened.");
	}
}