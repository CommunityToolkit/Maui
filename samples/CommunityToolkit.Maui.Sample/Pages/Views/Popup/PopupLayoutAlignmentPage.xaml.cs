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
		var verticalOptions = LayoutOptions.Start;
		var horizontalOptions = LayoutOptions.Start;

		if (VerticalOptionsStartRadioButton.IsChecked)
		{
			verticalOptions = LayoutOptions.Start;
		}
		else if (VerticalOptionsCenterRadioButton.IsChecked)
		{
			verticalOptions = LayoutOptions.Center;
		}
		else if (VerticalOptionsEndRadioButton.IsChecked)
		{
			verticalOptions = LayoutOptions.End;
		}
		else if (VerticalOptionsFillRadioButton.IsChecked)
		{
			verticalOptions = LayoutOptions.Fill;
		}
		else
		{
			throw new InvalidOperationException("VerticalOptions Radio Button Must Be Selected");
		}

		if (HorizontalOptionsStartRadioButton.IsChecked)
		{
			horizontalOptions = LayoutOptions.Start;
		}
		else if (HorizontalOptionsCenterRadioButton.IsChecked)
		{
			horizontalOptions = LayoutOptions.Center;
		}
		else if (HorizontalOptionsEndRadioButton.IsChecked)
		{
			horizontalOptions = LayoutOptions.End;
		}
		else if (HorizontalOptionsFillRadioButton.IsChecked)
		{
			horizontalOptions = LayoutOptions.Fill;
		}
		else
		{
			throw new InvalidOperationException("HorizontalOptions Radio Button Must Be Selected");
		}

		await Navigation.ShowPopup<RedBlueBoxPopup>(new RedBlueBoxPopup()
		{
			WidthRequest = double.Parse(widthEntry.Text),
			HeightRequest = double.Parse(heightEntry.Text)
		}, new PopupOptions() { VerticalOptions = verticalOptions, HorizontalOptions = horizontalOptions });
	}
}