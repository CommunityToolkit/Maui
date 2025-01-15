using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupLayoutAlignmentPage : BasePage<PopupLayoutAlignmentViewModel>
{
	public PopupLayoutAlignmentPage(PopupLayoutAlignmentViewModel popupLayoutViewModel) : base(popupLayoutViewModel)
	{
		InitializeComponent();
	}

	async void ShowPopupButtonClicked(object sender, EventArgs e)
	{
		var redBlueBoxPopup = new RedBlueBoxPopup
		{
			WidthRequest = double.Parse(widthEntry.Text),
			HeightRequest = double.Parse(heightEntry.Text)
		};

		await Navigation.ShowPopup(redBlueBoxPopup, new PopupOptions<RedBlueBoxPopup>());
	}
}