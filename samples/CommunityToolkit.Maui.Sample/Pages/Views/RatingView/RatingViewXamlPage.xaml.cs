using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewXamlPage : BasePage<RatingViewXamlViewModel>
{
	readonly RatingViewXamlViewModel vm;

	public RatingViewXamlPage(RatingViewXamlViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		vm = viewModel;
	}

	void StepperMaximumRating_RatingChanged(object sender, RatingChangedEventArgs e)
	{
		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		if (sender is RatingView ratingView)
		{
			_ = ratingView.Rating;
		}
	}

	void RatingViewShapePaddingLeft_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Left = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingTop_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Top = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingRight_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Right = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingBottom_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		Thickness currentThickness = vm.RatingViewShapePadding;
		currentThickness.Bottom = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}
}