using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// RangeSlider control
/// </summary>
public partial class RangeSlider : ContentView
{
	/// <summary>
	/// Gets or sets the minimum value
	/// </summary>
	[BindableProperty]
	public partial double MinimumValue { get; set; } = RangeSliderDefaults.MinimumValue;

	/// <summary>
	/// Gets or sets the maximum value
	/// </summary>
	[BindableProperty]
	public partial double MaximumValue { get; set; } = RangeSliderDefaults.MaximumValue;

	/// <summary>
	/// Gets or sets the lower value
	/// </summary>
	[BindableProperty(CoerceValueMethodName = nameof(CoerceLowerValue))]
	public partial double LowerValue { get; set; } = RangeSliderDefaults.LowerValue;

	/// <summary>
	/// Gets or sets the upper value
	/// </summary>
	[BindableProperty(CoerceValueMethodName = nameof(CoerceUpperValue))]
	public partial double UpperValue { get; set; } = RangeSliderDefaults.UpperValue;

	/// <summary>
	/// Gets or sets the step size
	/// </summary>
	[BindableProperty]
	public partial double StepSize { get; set; } = RangeSliderDefaults.StepSize;

	/// <summary>
	/// Gets or sets the lower thumb color
	/// </summary>
	[BindableProperty]
	public partial Color LowerThumbColor { get; set; } = RangeSliderDefaults.LowerThumbColor;

	/// <summary>
	/// Gets or sets the upper thumb color
	/// </summary>
	[BindableProperty]
	public partial Color UpperThumbColor { get; set; } = RangeSliderDefaults.UpperThumbColor;

	/// <summary>
	/// Gets or sets the inner track color
	/// </summary>
	[BindableProperty]
	public partial Color InnerTrackColor { get; set; } = RangeSliderDefaults.InnerTrackColor;

	/// <summary>
	/// Gets or sets the inner track size
	/// </summary>
	[BindableProperty]
	public partial double InnerTrackSize { get; set; } = RangeSliderDefaults.InnerTrackSize;

	/// <summary>
	/// Gets or sets the inner track corner radius
	/// </summary>
	[BindableProperty]
	public partial CornerRadius InnerTrackCornerRadius { get; set; } = RangeSliderDefaults.InnerTrackCornerRadius;

	/// <summary>
	/// Gets or sets the outer track color
	/// </summary>
	[BindableProperty]
	public partial Color OuterTrackColor { get; set; } = RangeSliderDefaults.OuterTrackColor;

	/// <summary>
	/// Gets or sets the outer track size
	/// </summary>
	[BindableProperty]
	public partial double OuterTrackSize { get; set; } = RangeSliderDefaults.OuterTrackSize;

	/// <summary>
	/// Gets or sets the outer track corner radius
	/// </summary>
	[BindableProperty]
	public partial CornerRadius OuterTrackCornerRadius { get; set; } = RangeSliderDefaults.OuterTrackCornerRadius;

	internal static readonly BindablePropertyKey FocusModePropertyKey = BindableProperty.CreateReadOnly(nameof(FocusMode), typeof(RangeSliderFocusMode), typeof(RangeSlider), RangeSliderDefaults.FocusMode);

	/// <summary>
	/// This is a read-only <see cref="FocusMode"/> property that represents the focus state of the slider thumbs.
	/// </summary>
	public static readonly BindableProperty FocusModeProperty = FocusModePropertyKey.BindableProperty;

	/// <summary>
	/// Gets the current focus state of the slider thumbs.
	/// </summary>
	public RangeSliderFocusMode FocusMode => (RangeSliderFocusMode)GetValue(FocusModeProperty);

	/// <summary>
	/// Gets the platform-specific thumb size measured from the underlying .NET MAUI Slider thumb.
	/// It was observed that the thumb size differs on Windows compared to other platforms.
	/// </summary>
#if WINDOWS
	public const double PlatformThumbSize = 17.5;
#else
	public const double PlatformThumbSize = 31.5;
#endif

	readonly RoundRectangle outerTrack = new()
	{
		HorizontalOptions = LayoutOptions.Start,
		VerticalOptions = LayoutOptions.Center,
	};

	readonly RoundRectangle innerTrack = new()
	{
		HorizontalOptions = LayoutOptions.Start,
		VerticalOptions = LayoutOptions.Center,
	};

	readonly Slider lowerSlider = new()
	{
		HorizontalOptions = LayoutOptions.Start,
		VerticalOptions = LayoutOptions.Center,
		BackgroundColor = Colors.Transparent,
		MinimumTrackColor = Colors.Transparent,
		MaximumTrackColor = Colors.Transparent,
	};

	readonly Slider upperSlider = new()
	{
		HorizontalOptions = LayoutOptions.End,
		VerticalOptions = LayoutOptions.Center,
		BackgroundColor = Colors.Transparent,
		MinimumTrackColor = Colors.Transparent,
		MaximumTrackColor = Colors.Transparent,
	};

	/// <summary>
	/// Internal property used to defer clamping until initialization completes so all bindable properties, bindings, and default values settle before coercion runs.
	/// </summary>
	internal bool IsClampingEnabled { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="RangeSlider"/> class
	/// </summary>
	public RangeSlider()
	{
		PropertyChanged += HandlePropertyChanged;

		WidthRequest = RangeSliderDefaults.WidthRequest;
		outerTrack.SetBinding(RoundRectangle.HeightRequestProperty, BindingBase.Create<RangeSlider, double>(static p => p.OuterTrackSize, BindingMode.OneWay, source: this));
		outerTrack.SetBinding(RoundRectangle.BackgroundColorProperty, BindingBase.Create<RangeSlider, Color>(static p => p.OuterTrackColor, BindingMode.OneWay, source: this));
		outerTrack.SetBinding(RoundRectangle.CornerRadiusProperty, BindingBase.Create<RangeSlider, CornerRadius>(static p => p.OuterTrackCornerRadius, BindingMode.OneWay, source: this));
		innerTrack.SetBinding(RoundRectangle.HeightRequestProperty, BindingBase.Create<RangeSlider, double>(static p => p.InnerTrackSize, BindingMode.OneWay, source: this));
		innerTrack.SetBinding(RoundRectangle.BackgroundColorProperty, BindingBase.Create<RangeSlider, Color>(static p => p.InnerTrackColor, BindingMode.OneWay, source: this));
		innerTrack.SetBinding(RoundRectangle.CornerRadiusProperty, BindingBase.Create<RangeSlider, CornerRadius>(static p => p.InnerTrackCornerRadius, BindingMode.OneWay, source: this));
		lowerSlider.SetBinding(Slider.ThumbColorProperty, BindingBase.Create<RangeSlider, Color>(static p => p.LowerThumbColor, BindingMode.OneWay, source: this));
		upperSlider.SetBinding(Slider.ThumbColorProperty, BindingBase.Create<RangeSlider, Color>(static p => p.UpperThumbColor, BindingMode.OneWay, source: this));

		Content = new Grid
		{
			Children =
			{
				outerTrack,
				innerTrack,
				lowerSlider,
				upperSlider,
			},
		};

		Loaded += FinalizeInitialization;
	}

	/// <summary>
	/// Initializes a new instance of the RangeSlider class for unit testing purposes.
	/// </summary>
	/// <param name="unitTest">The value is irrelevant - passing this parameter enables the class for unit testing scenarios.</param>
	internal RangeSlider(bool unitTest) : this()
	{
		FinalizeInitialization(this, EventArgs.Empty);
	}

	/// <summary>
	/// Enables clamping and finalizes setup after all bindable properties, bindings, and defaults have been initialized.
	/// Once initialization completes, coerces <see cref="LowerValue"/> and <see cref="UpperValue"/> into valid, clamped ranges,
	/// wires up event handlers, and updates slider ranges, values, focus state, and track layouts.
	/// </summary>
	void FinalizeInitialization(object? sender, EventArgs e)
	{
		IsClampingEnabled = true;
		CoerceValue(LowerValueProperty);
		CoerceValue(UpperValueProperty);
		lowerSlider.DragStarted += HandleLowerSliderDragStarted;
		lowerSlider.DragCompleted += HandleLowerSliderDragCompleted;
		lowerSlider.PropertyChanged += HandleLowerSliderPropertyChanged;
		lowerSlider.Focused += HandleLowerSliderFocused;
		upperSlider.DragStarted += HandleUpperSliderDragStarted;
		upperSlider.DragCompleted += HandleUpperSliderDragCompleted;
		upperSlider.PropertyChanged += HandleUpperSliderPropertyChanged;
		upperSlider.Focused += HandleUpperSliderFocused;
		UpdateSliderRanges();
		UpdateLowerSliderValue();
		UpdateUpperSliderValue();
		UpdateFocusedSliderLayout();
		UpdateOuterTrackLayout();
		UpdateInnerTrackLayout();
	}

	/// <summary>
	/// This method changes the value of the <see cref="FocusMode"/> property.
	/// </summary>
	/// <param name="focusMode">The new focus mode</param>
	protected void SetFocusMode(RangeSliderFocusMode focusMode) => SetValue(FocusModePropertyKey, focusMode);

	static object CoerceLowerValue(BindableObject bindable, object value)
	{
		var rangeSlider = (RangeSlider)bindable;
		var input = (double)value;

		if (rangeSlider.IsClampingEnabled)
		{
			if (rangeSlider.MinimumValue <= rangeSlider.MaximumValue)
			{
				input = Math.Min(Math.Clamp(input, rangeSlider.MinimumValue, rangeSlider.MaximumValue), rangeSlider.UpperValue);
			}
			else
			{
				input = Math.Max(Math.Clamp(input, rangeSlider.MaximumValue, rangeSlider.MinimumValue), rangeSlider.UpperValue);
			}
		}

		return input;
	}

	static object CoerceUpperValue(BindableObject bindable, object value)
	{
		var rangeSlider = (RangeSlider)bindable;
		var input = (double)value;

		if (rangeSlider.IsClampingEnabled)
		{
			if (rangeSlider.MinimumValue <= rangeSlider.MaximumValue)
			{
				input = Math.Max(Math.Clamp(input, rangeSlider.MinimumValue, rangeSlider.MaximumValue), rangeSlider.LowerValue);
			}
			else
			{
				input = Math.Min(Math.Clamp(input, rangeSlider.MaximumValue, rangeSlider.MinimumValue), rangeSlider.LowerValue);
			}
		}

		return input;
	}

	void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.PropertyName))
		{
			return;
		}

		if (e.PropertyName.IsOneOf(MinimumValueProperty, MaximumValueProperty, StepSizeProperty))
		{
			UpdateSliderRanges();
		}

		if (e.PropertyName.IsOneOf(WidthProperty, MinimumValueProperty, MaximumValueProperty, StepSizeProperty))
		{
			UpdateFocusedSliderLayout();
		}

		if (e.PropertyName.Is(LowerValueProperty))
		{
			UpdateLowerSliderValue();
		}

		if (e.PropertyName.Is(UpperValueProperty))
		{
			UpdateUpperSliderValue();
		}

		if (e.PropertyName.IsOneOf(WidthProperty, MinimumValueProperty, MaximumValueProperty, OuterTrackSizeProperty))
		{
			UpdateOuterTrackLayout();
		}

		if (e.PropertyName.IsOneOf(WidthProperty, MinimumValueProperty, MaximumValueProperty, LowerValueProperty, UpperValueProperty, InnerTrackSizeProperty))
		{
			UpdateInnerTrackLayout();
		}
	}

	void HandleLowerSliderDragStarted(object? sender, EventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Lower);
		UpdateFocusedSliderLayout();
	}

	void HandleLowerSliderDragCompleted(object? sender, EventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Default);
		UpdateFocusedSliderLayout();
	}

	/// <summary>
	/// Computes the effective unit used for slider value calculations.
	/// </summary>
	/// <remarks>
	/// The returned unit is derived from <see cref="StepSize"/> and corrected for the sign of the range:
	/// - If <see cref="StepSize"/> is 0, defaults to 1 (no stepping).
	/// - Otherwise uses the absolute value of <see cref="StepSize"/>.
	/// - If <see cref="MinimumValue"/> is greater than <see cref="MaximumValue"/>, the unit is negated to match a descending range.
	/// This ensures consistent mapping between slider positions and <see cref="LowerValue"/>/<see cref="UpperValue"/> across ascending and descending ranges.
	/// </remarks>
	/// <returns>
	/// A positive unit for ascending ranges or a negative unit for descending ranges. Defaults to 1 or -1 when <see cref="StepSize"/> is 0.
	/// </returns>
	double GetUnit()
	{
		double unit = StepSize == 0 ? 1 : Math.Abs(StepSize);
		return MinimumValue <= MaximumValue ? unit : -unit;
	}

	void HandleLowerSliderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.PropertyName))
		{
			return;
		}

		if (e.PropertyName.Is(Slider.ValueProperty))
		{
			LowerValue = CalculateValueFromSliderValue(lowerSlider.Value);
			SetFocusMode(RangeSliderFocusMode.Lower);
			UpdateInnerTrackLayout();
		}
	}

	void HandleLowerSliderFocused(object? sender, FocusEventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Lower);
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderDragStarted(object? sender, EventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Upper);
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderDragCompleted(object? sender, EventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Default);
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (string.IsNullOrWhiteSpace(e.PropertyName))
		{
			return;
		}

		if (e.PropertyName.Is(Slider.ValueProperty))
		{
			UpperValue = CalculateValueFromSliderValue(upperSlider.Value);
			SetFocusMode(RangeSliderFocusMode.Upper);
			UpdateInnerTrackLayout();
		}
	}

	double CalculateValueFromSliderValue(double sliderValue)
	{
		return (StepSize == 0.0 ? sliderValue : Math.Round(sliderValue)) * GetUnit() + MinimumValue;
	}

	void HandleUpperSliderFocused(object? sender, FocusEventArgs e)
	{
		SetFocusMode(RangeSliderFocusMode.Upper);
		UpdateFocusedSliderLayout();
	}

	void UpdateFocusedSliderLayout()
	{
		double unit = GetUnit();
		double maxValue = (MaximumValue - MinimumValue) / unit;
		double middleValue = FocusMode switch
		{
			RangeSliderFocusMode.Lower => upperSlider.Value,
			RangeSliderFocusMode.Upper => lowerSlider.Value,
			_ => (lowerSlider.Value + upperSlider.Value) / 2,
		};
		lowerSlider.Minimum = 0;
		lowerSlider.Maximum = middleValue;
		upperSlider.Minimum = middleValue;
		upperSlider.Maximum = maxValue;

		double range = upperSlider.Maximum - lowerSlider.Minimum;
		double trackWidth = Width - PlatformThumbSize;
		if (IsLayoutValid(range, trackWidth))
		{
			lowerSlider.WidthRequest = trackWidth * (lowerSlider.Maximum - lowerSlider.Minimum) / range + PlatformThumbSize;
			upperSlider.WidthRequest = trackWidth * (upperSlider.Maximum - upperSlider.Minimum) / range + PlatformThumbSize;
		}
	}

	void UpdateSliderRanges()
	{
		double unit = GetUnit();
		double maxValue = (MaximumValue - MinimumValue) / unit;
		lowerSlider.Minimum = 0;
		lowerSlider.Maximum = maxValue;
		upperSlider.Minimum = 0;
		upperSlider.Maximum = maxValue;
	}

	void UpdateLowerSliderValue()
	{
		lowerSlider.Value = (LowerValue - MinimumValue) / GetUnit();
	}

	void UpdateUpperSliderValue()
	{
		upperSlider.Value = (UpperValue - MinimumValue) / GetUnit();
	}

	void UpdateOuterTrackLayout()
	{
		outerTrack.TranslationX = PlatformThumbSize / 2 - OuterTrackSize / 2;
		outerTrack.WidthRequest = (Width - PlatformThumbSize) + OuterTrackSize;
	}

	void UpdateInnerTrackLayout()
	{
		double range = upperSlider.Maximum - lowerSlider.Minimum;
		double trackWidth = Width - PlatformThumbSize;
		if (IsLayoutValid(range, trackWidth))
		{
			innerTrack.TranslationX = trackWidth * (lowerSlider.Value - lowerSlider.Minimum) / range + PlatformThumbSize / 2 - InnerTrackSize / 2;
			innerTrack.WidthRequest = trackWidth * (upperSlider.Value - lowerSlider.Value) / range + InnerTrackSize;
		}
	}

	bool IsLayoutValid(double range, double trackWidth)
	{
		return range != 0 && trackWidth > 0;
	}
}
