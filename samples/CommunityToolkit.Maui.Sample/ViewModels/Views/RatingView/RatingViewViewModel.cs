// Ignore Spelling: csharp, color, colors

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewXamlViewModel : BaseRatingViewViewModel;
public partial class RatingViewCsharpViewModel : BaseRatingViewViewModel;

public abstract partial class BaseRatingViewViewModel : BaseViewModel
{
	static readonly ReadOnlyDictionary<string, Color> colorList = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(static c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.AsReadOnly();

	static readonly ImmutableList<string> colorsForPickers = [.. colorList.Keys];

	public IReadOnlyList<string> ColorsForPickers => [.. colorsForPickers];

	public Thickness RatingViewShapePaddingValue => new(RatingViewShapePaddingLeft, RatingViewShapePaddingTop, RatingViewShapePaddingRight, RatingViewShapePaddingBottom);

	public Color ColorPickerEmptyBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerEmptyBackgroundSelectedIndex).Value;

	public Color ColorPickerRatingShapeBorderColorTarget => colorList.ElementAtOrDefault(ColorPickerRatingShapeBorderColorSelectedIndex).Value;

	public Color ColorPickerFilledBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerFilledBackgroundSelectedIndex).Value;

	[ObservableProperty]
	public partial double StepperValueMaximumRatings { get; set; } = 1;

	[ObservableProperty]
	public partial Thickness RatingViewShapePadding { get; set; } = new(0);

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerFilledBackgroundTarget))]
	public partial int ColorPickerFilledBackgroundSelectedIndex { get; set; } = colorsForPickers.IndexOf(nameof(Colors.Red));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerEmptyBackgroundTarget))]
	public partial int ColorPickerEmptyBackgroundSelectedIndex { get; set; } = colorsForPickers.IndexOf(nameof(Colors.Green));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(ColorPickerRatingShapeBorderColorTarget))]
	public partial int ColorPickerRatingShapeBorderColorSelectedIndex { get; set; } = colorsForPickers.IndexOf(nameof(Colors.Blue));

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	public partial double RatingViewShapePaddingLeft { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	public partial double RatingViewShapePaddingTop { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	public partial double RatingViewShapePaddingRight { get; set; }

	[ObservableProperty, NotifyPropertyChangedFor(nameof(RatingViewShapePaddingValue))]
	public partial double RatingViewShapePaddingBottom { get; set; }
}