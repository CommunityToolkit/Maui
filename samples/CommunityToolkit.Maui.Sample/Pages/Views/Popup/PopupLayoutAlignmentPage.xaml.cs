using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Sample.Views.Popups;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class PopupLayoutAlignmentPage : BasePage<PopupLayoutAlignmentViewModel>
{
	public PopupLayoutAlignmentPage(PopupLayoutAlignmentViewModel popupLayoutViewModel) : base(popupLayoutViewModel)
	{
		InitializeComponent();
	}

	void ShowPopupButtonClicked(object? sender, EventArgs? e)
	{
		LayoutOptions verticalOptions, horizontalOptions;

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
		else
		{
			throw new InvalidOperationException("HorizontalOptions Radio Button Must Be Selected");
		}

		this.ShowPopup(new RedBlueBoxPopup
		{
			VerticalOptions = verticalOptions,
			HorizontalOptions = horizontalOptions,
			Padding = 0,
			WidthRequest = double.Parse(widthEntry.Text),
			HeightRequest = double.Parse(heightEntry.Text)
		}, new PopupOptions
		{
			Shape = null
		});
	}
}