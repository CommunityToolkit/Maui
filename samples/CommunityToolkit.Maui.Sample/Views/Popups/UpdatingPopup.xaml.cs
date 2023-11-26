using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Views.Popups;

public partial class UpdatingPopup : Popup
{
	public UpdatingPopup(UpdatingPopupViewModel updatingPopupViewModel)
	{
		InitializeComponent();
		BindingContext = updatingPopupViewModel;

		updatingPopupViewModel.Finished += OnUpdatingPopupViewModelFinished;
	}

	void OnUpdatingPopupViewModelFinished(object? sender, EventArgs e)
	{
		this.Close();
	}
}