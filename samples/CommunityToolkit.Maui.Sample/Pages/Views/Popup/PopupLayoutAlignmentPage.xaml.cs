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
		var redBlueBoxPopupOptions = new PopupOptions();

		if (VerticalOptionsStartRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.VerticalOptions = LayoutOptions.Start;
		}
		else if (VerticalOptionsCenterRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.VerticalOptions = LayoutOptions.Center;
		}
		else if (VerticalOptionsEndRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.VerticalOptions = LayoutOptions.End;
		}
		else if (VerticalOptionsFillRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.VerticalOptions = LayoutOptions.Fill;
		}
		else
		{
			throw new InvalidOperationException("VerticalOptions Radio Button Must Be Selected");
		}

		if (HorizontalOptionsStartRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.HorizontalOptions = LayoutOptions.Start;
		}
		else if (HorizontalOptionsCenterRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.HorizontalOptions = LayoutOptions.Center;
		}
		else if (HorizontalOptionsEndRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.HorizontalOptions = LayoutOptions.End;
		}
		else if (HorizontalOptionsFillRadioButton.IsChecked)
		{
			redBlueBoxPopupOptions.HorizontalOptions = LayoutOptions.Fill;
		}
		else
		{
			throw new InvalidOperationException("HorizontalOptions Radio Button Must Be Selected");
		}

		await Navigation.ShowPopup<RedBlueBoxPopup>(new RedBlueBoxPopup()
		{
			WidthRequest = double.Parse(widthEntry.Text),
			HeightRequest = double.Parse(heightEntry.Text)
		}, redBlueBoxPopupOptions);
	}
}