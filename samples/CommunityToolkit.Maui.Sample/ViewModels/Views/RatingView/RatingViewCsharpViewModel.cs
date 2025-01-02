// Ignore Spelling: csharp, color, colors

using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CommunityToolkit.Maui.Sample.ViewModels.Views;

public partial class RatingViewCsharpViewModel : BaseViewModel
{
	static readonly ReadOnlyDictionary<string, Color> colorList = typeof(Colors)
		.GetFields(BindingFlags.Static | BindingFlags.Public)
		.ToDictionary(static c => c.Name, c => (Color)(c.GetValue(null) ?? throw new InvalidOperationException()))
		.AsReadOnly();
	static readonly ImmutableList<string> colorsForPickers = [.. colorList.Keys];
	Thickness ratingViewShapePadding = new(0);

	public double StepperValueMaximumRatings
	{
		get;
		set => SetProperty(ref field, value);
	} = 1;

	public Thickness RatingViewShapePadding
	{
		get => ratingViewShapePadding;
		set => SetProperty(ref ratingViewShapePadding, value);
	}

	public int ColorPickerFilledBackgroundSelectedIndex
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(ColorPickerFilledBackgroundTarget));
			}
		}
	} = colorsForPickers.IndexOf(nameof(Colors.Red));

	public int ColorPickerEmptyBackgroundSelectedIndex
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(ColorPickerEmptyBackgroundTarget));
			}
		}
	} = colorsForPickers.IndexOf(nameof(Colors.Green));

	public int ColorPickerRatingShapeBorderColorSelectedIndex
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(ColorPickerRatingShapeBorderColorTarget));
			}
		}
	} = colorsForPickers.IndexOf(nameof(Colors.Blue));

	public double RatingViewShapePaddingLeft
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(RatingViewShapePaddingValue));
			}
		}
	}

	public double RatingViewShapePaddingTop
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(RatingViewShapePaddingValue));
			}
		}
	}

	public double RatingViewShapePaddingRight
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(RatingViewShapePaddingValue));
			}
		}
	}

	public double RatingViewShapePaddingBottom
	{
		get;
		set
		{
			if (SetProperty(ref field, value))
			{
				OnPropertyChanged(nameof(RatingViewShapePaddingValue));
			}
		}
	}

	public IReadOnlyList<string> ColorsForPickers => [.. colorsForPickers];

	public Thickness RatingViewShapePaddingValue => new(RatingViewShapePaddingLeft, RatingViewShapePaddingTop, RatingViewShapePaddingRight, RatingViewShapePaddingBottom);

	public Color ColorPickerEmptyBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerEmptyBackgroundSelectedIndex).Value;

	public Color ColorPickerRatingShapeBorderColorTarget => colorList.ElementAtOrDefault(ColorPickerRatingShapeBorderColorSelectedIndex).Value;

	public Color ColorPickerFilledBackgroundTarget => colorList.ElementAtOrDefault(ColorPickerFilledBackgroundSelectedIndex).Value;
}