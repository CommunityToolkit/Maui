using System.Reflection;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class RatingViewXamlPage : BasePage<RatingViewXamlViewModel>
{
	RatingViewXamlViewModel vm;
	readonly IReadOnlyDictionary<string, Color> colors = typeof(Colors).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	public RatingViewXamlPage(RatingViewXamlViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();
		vm = viewModel;
	}

	protected override void OnAppearing()
	{
		List<string> colorKeys = colors.Keys.ToList();
		ColorPickerEmptyBackground.ItemsSource = colorKeys;
		ColorPickerFilledBackground.ItemsSource = colorKeys;
		ColorPickerRatingShapeBorderColor.ItemsSource = colorKeys;
	}

	void ColorPicker_SelectedIndexChanged(object sender, EventArgs e)
	{
		Color color = colors.ElementAtOrDefault(ColorPickerEmptyBackground.SelectedIndex).Value ?? Colors.Transparent;
		switch (((Picker)sender).StyleId)
		{
			case "ColorPickerEmptyBackground":
				ColorPickerEmptyBackgroundTarget.EmptyBackgroundColor = color;
				break;
			case "ColorPickerFilledBackground":
				ColorPickerFilledBackgroundTarget.FilledBackgroundColor = color;
				break;
			case "ColorPickerRatingShapeBorderColor":
				ColorPickerRatingShapeBorderColorTarget.RatingShapeOutlineColor = color;
				break;
			default:
				break;
		}
	}

	void StepperMaximumRating_RatingChanged(object sender, EventArgs e)
	{
		// This is the weak event raised when the rating is changed.  The developer can then perform further actions (such as save to DB).
		if (sender is RatingView ratingView)
		{
			_ = ratingView.Rating;
		}
	}

	void RatingViewShapePaddingLeft_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		var currentThickness = vm.RatingViewShapePadding;
		currentThickness.Left = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingTop_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		var currentThickness = vm.RatingViewShapePadding;
		currentThickness.Top = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingRight_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		var currentThickness = vm.RatingViewShapePadding;
		currentThickness.Right = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}

	void RatingViewShapePaddingBottom_ValueChanged(object sender, ValueChangedEventArgs e)
	{
		var currentThickness = vm.RatingViewShapePadding;
		currentThickness.Bottom = e.NewValue;
		vm.RatingViewShapePadding = currentThickness;
	}
}