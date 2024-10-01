// Ignore Spelling: csharp

using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewCsharpViewModel : BaseViewModel
{
	readonly ReadOnlyDictionary<string, Color> colorList = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(static c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.AsReadOnly();

	public RatingViewCsharpViewModel()
	{
		ColorsForPickers = [.. colorList.Keys];
		ColorPickerEmptyBackgroundSelectedIndex = ColorsForPickers.IndexOf("Red");
		ColorPickerFilledBackgroundSelectedIndex = ColorsForPickers.IndexOf("Green");
		ColorPickerRatingShapeBorderColorSelectedIndex = ColorsForPickers.IndexOf("Blue");
	}

	[ObservableProperty]
	List<string> colorsForPickers;

	[ObservableProperty]
	double stepperValueMaximumRatings = 1;

	[ObservableProperty]
	Thickness ratingViewShapePadding = new(0);

	[ObservableProperty]
	Color? colorPickerFilledBackgroundTarget = Colors.Green,
		colorPickerEmptyBackgroundTarget = Colors.Red,
		colorPickerRatingShapeBorderColorTarget = Colors.Blue;

	[ObservableProperty]
	int? colorPickerFilledBackgroundSelectedIndex, colorPickerEmptyBackgroundSelectedIndex, colorPickerRatingShapeBorderColorSelectedIndex;

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	double ratingViewShapePaddingLeft, ratingViewShapePaddingTop, ratingViewShapePaddingRight, ratingViewShapePaddingBottom;

	public Thickness RatingViewShapePaddingValue => new(RatingViewShapePaddingLeft, RatingViewShapePaddingTop, RatingViewShapePaddingRight, RatingViewShapePaddingBottom);

	[RelayCommand]
	void ColorPickerFilledBackground()
	{
		if (ColorPickerFilledBackgroundSelectedIndex is not null)
		{
			ColorPickerFilledBackgroundTarget = colorList.ElementAtOrDefault(ColorPickerFilledBackgroundSelectedIndex.Value).Value;
		}
	}

	[RelayCommand]
	void ColorPickerEmptyBackground()
	{
		if (ColorPickerEmptyBackgroundSelectedIndex is not null)
		{
			ColorPickerEmptyBackgroundTarget = colorList.ElementAtOrDefault(ColorPickerEmptyBackgroundSelectedIndex.Value).Value;
		}
	}

	[RelayCommand]
	void ColorPickerRatingShapeBorderColor()
	{
		if (ColorPickerRatingShapeBorderColorSelectedIndex is not null)
		{
			ColorPickerRatingShapeBorderColorTarget = colorList.ElementAtOrDefault(ColorPickerRatingShapeBorderColorSelectedIndex.Value).Value;
		}
	}
}