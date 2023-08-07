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

	void ShowPopupButtonClicked(object sender, EventArgs e)
	{
		var redBlueBoxPopup = new RedBlueBoxPopup
		{
			Size = new Size(double.Parse(widthEntry.Text), double.Parse(heightEntry.Text)),
		};

		if (VerticalOptionsStartRadioButton.IsChecked)
		{
			redBlueBoxPopup.VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Start;
		}
		else if (VerticalOptionsCenterRadioButton.IsChecked)
		{
			redBlueBoxPopup.VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center;
		}
		else if (VerticalOptionsEndRadioButton.IsChecked)
		{
			redBlueBoxPopup.VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.End;
		}
		else if (VerticalOptionsFillRadioButton.IsChecked)
		{
			redBlueBoxPopup.VerticalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		}
		else
		{
			throw new InvalidOperationException("VerticalOptions Radio Button Must Be Selected");
		}

		if (HorizontalOptionsStartRadioButton.IsChecked)
		{
			redBlueBoxPopup.HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Start;
		}
		else if (HorizontalOptionsCenterRadioButton.IsChecked)
		{
			redBlueBoxPopup.HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Center;
		}
		else if (HorizontalOptionsEndRadioButton.IsChecked)
		{
			redBlueBoxPopup.HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.End;
		}
		else if (HorizontalOptionsFillRadioButton.IsChecked)
		{
			redBlueBoxPopup.HorizontalOptions = Microsoft.Maui.Primitives.LayoutAlignment.Fill;
		}
		else
		{
			throw new InvalidOperationException("HorizontalOptions Radio Button Must Be Selected");
		}

		this.ShowPopup(redBlueBoxPopup);
	}
}