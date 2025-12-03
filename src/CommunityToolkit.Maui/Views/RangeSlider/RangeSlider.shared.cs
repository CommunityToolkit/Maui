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
	[BindableProperty(DefaultValue = RangeSliderDefaults.MinimumValue)]
	public partial double MinimumValue { get; set; }

	/// <summary>
	/// Gets or sets the maximum value
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.MaximumValue)]
	public partial double MaximumValue { get; set; }

	/// <summary>
	/// Gets or sets the lower value
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.LowerValue, CoerceValueMethodName = nameof(CoerceLowerValue))]
	public partial double LowerValue { get; set; }

	/// <summary>
	/// Gets or sets the upper value
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.UpperValue, CoerceValueMethodName = nameof(CoerceUpperValue))]
	public partial double UpperValue { get; set; }

	/// <summary>
	/// Gets or sets the step size
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.StepSize)]
	public partial double StepSize { get; set; }

	/// <summary>
	/// Gets or sets the lower thumb color
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultLowerThumbColor))]
	public partial Color LowerThumbColor { get; set; }

	/// <summary>
	/// Gets or sets the upper thumb color
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultUpperThumbColor))]
	public partial Color UpperThumbColor { get; set; }

	/// <summary>
	/// Gets or sets the inner track color
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultInnerTrackColor))]
	public partial Color InnerTrackColor { get; set; }

	/// <summary>
	/// Gets or sets the inner track size
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.InnerTrackSize)]
	public partial double InnerTrackSize { get; set; }

	/// <summary>
	/// Gets or sets the inner track corner radius
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultInnerTrackCornerRadius))]
	public partial CornerRadius InnerTrackCornerRadius { get; set; }

	/// <summary>
	/// Gets or sets the outer track color
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultOuterTrackColor))]
	public partial Color OuterTrackColor { get; set; }

	/// <summary>
	/// Gets or sets the outer track size
	/// </summary>
	[BindableProperty(DefaultValue = RangeSliderDefaults.OuterTrackSize)]
	public partial double OuterTrackSize { get; set; }

	/// <summary>
	/// Gets or sets the outer track corner radius
	/// </summary>
	[BindableProperty(DefaultValueCreatorMethodName = nameof(CreateDefaultOuterTrackCornerRadius))]
	public partial CornerRadius OuterTrackCornerRadius { get; set; }

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
	/// Gets the platform-specific thumb size
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
	/// Defer clamping until initialization completes so all bindable properties, bindings, and default values settle before coercion runs.
	/// </summary>
	bool isClampingEnabled;

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

		Dispatcher.Dispatch(FinalizeInitialization);
	}

	/// <summary>
	/// Enables clamping and finalizes setup after all bindable properties, bindings, and defaults have been initialized.
	/// Once initialization completes, coerces <see cref="LowerValue"/> and <see cref="UpperValue"/> into valid, clamped ranges,
	/// wires up event handlers, and updates slider ranges, values, focus state, and track layouts.
	/// </summary>
	void FinalizeInitialization()
	{
		isClampingEnabled = true;
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

		if (rangeSlider.isClampingEnabled)
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

		if (rangeSlider.isClampingEnabled)
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
			LowerValue = (StepSize == 0.0 ? lowerSlider.Value : Math.Round(lowerSlider.Value)) * GetUnit() + MinimumValue;
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
			UpperValue = (StepSize == 0.0 ? upperSlider.Value : Math.Round(upperSlider.Value)) * GetUnit() + MinimumValue;
			SetFocusMode(RangeSliderFocusMode.Upper);
			UpdateInnerTrackLayout();
		}
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
		if (range != 0)
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
		if (range != 0)
		{
			innerTrack.TranslationX = (Width - PlatformThumbSize) * (lowerSlider.Value - lowerSlider.Minimum) / range + PlatformThumbSize / 2 - InnerTrackSize / 2;
			innerTrack.WidthRequest = (Width - PlatformThumbSize) * (upperSlider.Value - lowerSlider.Value) / range + InnerTrackSize;
		}
	}

	static object CreateDefaultLowerThumbColor(BindableObject bindable) => RangeSliderDefaults.LowerThumbColor;

	static object CreateDefaultUpperThumbColor(BindableObject bindable) => RangeSliderDefaults.UpperThumbColor;

	static object CreateDefaultInnerTrackColor(BindableObject bindable) => RangeSliderDefaults.InnerTrackColor;

	static object CreateDefaultInnerTrackCornerRadius(BindableObject bindable) => RangeSliderDefaults.InnerTrackCornerRadius;

	static object CreateDefaultOuterTrackColor(BindableObject bindable) => RangeSliderDefaults.OuterTrackColor;

	static object CreateDefaultOuterTrackCornerRadius(BindableObject bindable) => RangeSliderDefaults.OuterTrackCornerRadius;
}
