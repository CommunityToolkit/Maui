// Ignore Spelling: csharp
namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui;

public partial class RatingViewXamlViewModel : BaseViewModel
{
	public RatingViewXamlViewModel()
	{
		ColorsForPickers = colorList.Keys.ToList();
		ColorPickerEmptyBackgroundSelectedIndex = ColorsForPickers.IndexOf("Red");
		ColorPickerFilledBackgroundSelectedIndex = ColorsForPickers.IndexOf("Green");
		ColorPickerRatingShapeBorderColorSelectedIndex = ColorsForPickers.IndexOf("Blue");
	}

	readonly IReadOnlyDictionary<string, Color> colorList = typeof(Colors).GetFields(BindingFlags.Static | BindingFlags.Public).ToDictionary(c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()));

	[ObservableProperty]
	List<string> colorsForPickers;

	[ObservableProperty]
	double stepperValueMaximumRatings = 1;

	[ObservableProperty]
	Thickness ratingViewShapePadding = new(0);

	[ObservableProperty]
	Color colorPickerFilledBackgroundTarget = Colors.Green;

	[ObservableProperty]
	Color colorPickerEmptyBackgroundTarget = Colors.Red;

	[ObservableProperty]
	Color colorPickerRatingShapeBorderColorTarget = Colors.Blue;

	[ObservableProperty]
	int colorPickerFilledBackgroundSelectedIndex = -1;

	[ObservableProperty]
	int colorPickerEmptyBackgroundSelectedIndex = -1;

	[ObservableProperty]
	int colorPickerRatingShapeBorderColorSelectedIndex = -1;

	[ObservableProperty]
	double ratingViewShapePaddingLeft = 0;

	[ObservableProperty]
	double ratingViewShapePaddingTop = 0;

	[ObservableProperty]
	double ratingViewShapePaddingRight = 0;

	[ObservableProperty]
	double ratingViewShapePaddingBottom = 0;

	[ObservableProperty]
	Thickness ratingViewShapePaddingValue = new(0);

	[RelayCommand]
	void ColorPickerFilledBackground()
	{
		if (ColorPickerFilledBackgroundSelectedIndex != -1)
		{
			ColorPickerFilledBackgroundTarget = colorList.ElementAtOrDefault(ColorPickerFilledBackgroundSelectedIndex).Value ?? Colors.Transparent;
		}
	}

	[RelayCommand]
	void ColorPickerEmptyBackground()
	{
		if (ColorPickerEmptyBackgroundSelectedIndex != -1)
		{
			ColorPickerEmptyBackgroundTarget = colorList.ElementAtOrDefault(ColorPickerEmptyBackgroundSelectedIndex).Value ?? Colors.Transparent;
		}
	}

	[RelayCommand]
	void ColorPickerRatingShapeBorderColor()
	{
		if (ColorPickerRatingShapeBorderColorSelectedIndex != -1)
		{
			ColorPickerRatingShapeBorderColorTarget = colorList.ElementAtOrDefault(ColorPickerRatingShapeBorderColorSelectedIndex).Value ?? Colors.Transparent;
		}
	}

	[RelayCommand]
	void RatingViewShapePaddingLeftCommand()
	{
		Thickness thickness = RatingViewShapePaddingValue;
		thickness.Left = RatingViewShapePaddingLeft;
		RatingViewShapePaddingValue = thickness;
	}

	[RelayCommand]
	void RatingViewShapePaddingTopCommand()
	{
		Thickness thickness = RatingViewShapePaddingValue;
		thickness.Top = RatingViewShapePaddingTop;
		RatingViewShapePaddingValue = thickness;
	}

	[RelayCommand]
	void RatingViewShapePaddingRightCommand()
	{
		Thickness thickness = RatingViewShapePaddingValue;
		thickness.Right = RatingViewShapePaddingRight;
		RatingViewShapePaddingValue = thickness;
	}

	[RelayCommand]
	void RatingViewShapePaddingBottomCommand()
	{
		Thickness thickness = RatingViewShapePaddingValue;
		thickness.Bottom = RatingViewShapePaddingBottom;
		RatingViewShapePaddingValue = thickness;
	}
}