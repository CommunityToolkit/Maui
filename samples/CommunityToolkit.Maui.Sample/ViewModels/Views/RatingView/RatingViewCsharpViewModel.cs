// Ignore Spelling: csharp

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewCsharpViewModel : BaseViewModel
{
	static readonly ReadOnlyDictionary<string, Color> colorList = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(static c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.AsReadOnly();

	static readonly ImmutableList<string> colorsForPickers = [..colorList.Keys];

	[ObservableProperty]
	double stepperValueMaximumRatings = 1;

	[ObservableProperty]
	Thickness ratingViewShapePadding = new(0);

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerFilledBackgroundTarget))]
	int colorPickerFilledBackgroundSelectedIndex = colorsForPickers.IndexOf(nameof(Colors.Red));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerEmptyBackgroundTarget))]
	int colorPickerEmptyBackgroundSelectedIndex = colorsForPickers.IndexOf(nameof(Colors.Green));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerRatingShapeBorderColorTarget))]
	int colorPickerRatingShapeBorderColorSelectedIndex = colorsForPickers.IndexOf(nameof(Colors.Blue));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	double ratingViewShapePaddingLeft, ratingViewShapePaddingTop, ratingViewShapePaddingRight, ratingViewShapePaddingBottom;

	public IReadOnlyList<string> ColorsForPickers => [..colorsForPickers];

	public Thickness RatingViewShapePaddingValue => new(RatingViewShapePaddingLeft, RatingViewShapePaddingTop, RatingViewShapePaddingRight, RatingViewShapePaddingBottom);

	public Color ColorPickerEmptyBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerEmptyBackgroundSelectedIndex).Value;

	public Color ColorPickerRatingShapeBorderColorTarget => colorList.ElementAtOrDefault(ColorPickerRatingShapeBorderColorSelectedIndex).Value;

	public Color ColorPickerFilledBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerFilledBackgroundSelectedIndex).Value;
}