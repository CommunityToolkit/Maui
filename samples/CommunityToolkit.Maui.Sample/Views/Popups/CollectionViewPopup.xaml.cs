using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class CollectionViewPopup : Popup<string>
{
	public CollectionViewPopup(CollectionViewPopupViewModel viewModel)
	{
		InitializeComponent();

		CanBeDismissedByTappingOutsideOfPopup = false;

		BindingContext = viewModel;
	}
}