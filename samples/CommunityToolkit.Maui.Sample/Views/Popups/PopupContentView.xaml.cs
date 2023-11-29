using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class PopupContentView : Grid
{
	public PopupContentView(PopupContentViewModel popupContentViewModel)
	{
		InitializeComponent();

		BindingContext = popupContentViewModel;
	}
}