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
				"This is a dynamically set message, shown in a popup without the need to create your own Popup subclass.");
	}
}