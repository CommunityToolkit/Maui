using System.ComponentModel;
using CommunityToolkit.Maui.Core;
using Microsoft.Maui.Controls.Shapes;

namespace CommunityToolkit.Maui.Views;

/// <summary>RangeSlider control.</summary>
public partial class RangeSlider : ContentView
{
	/// <summary>The backing store for the <see cref="MinimumValue" /> bindable property.</summary>
	public static readonly BindableProperty MinimumValueProperty = BindableProperty.Create(nameof(MinimumValue), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.MinimumValue);

	/// <summary>The backing store for the <see cref="MaximumValue" /> bindable property.</summary>
	public static readonly BindableProperty MaximumValueProperty = BindableProperty.Create(nameof(MaximumValue), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.MaximumValue);

	/// <summary>The backing store for the <see cref="LowerValue" /> bindable property.</summary>
	public static readonly BindableProperty LowerValueProperty = BindableProperty.Create(nameof(LowerValue), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.LowerValue, coerceValue: CoerceLowerValue);

	/// <summary>The backing store for the <see cref="UpperValue" /> bindable property.</summary>
	public static readonly BindableProperty UpperValueProperty = BindableProperty.Create(nameof(UpperValue), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.UpperValue, coerceValue: CoerceUpperValue);

	/// <summary>The backing store for the <see cref="StepSize"/> bindable property.</summary>
	public static readonly BindableProperty StepSizeProperty = BindableProperty.Create(nameof(StepSize), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.StepSize);

	/// <summary>The backing store for the <see cref="LowerThumbColor"/> bindable property.</summary>
	public static readonly BindableProperty LowerThumbColorProperty = BindableProperty.Create(nameof(LowerThumbColor), typeof(Color), typeof(RangeSlider), defaultValue: RangeSliderDefaults.LowerThumbColor);

	/// <summary>The backing store for the <see cref="UpperThumbColor"/> bindable property.</summary>
	public static readonly BindableProperty UpperThumbColorProperty = BindableProperty.Create(nameof(UpperThumbColor), typeof(Color), typeof(RangeSlider), defaultValue: RangeSliderDefaults.UpperThumbColor);

	/// <summary>The backing store for the <see cref="InnerTrackColor"/> bindable property.</summary>
	public static readonly BindableProperty InnerTrackColorProperty = BindableProperty.Create(nameof(InnerTrackColor), typeof(Color), typeof(RangeSlider), defaultValue: RangeSliderDefaults.InnerTrackColor);

	/// <summary>The backing store for the <see cref="InnerTrackSize"/> bindable property.</summary>
	public static readonly BindableProperty InnerTrackSizeProperty = BindableProperty.Create(nameof(InnerTrackSize), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.InnerTrackSize);

	/// <summary>The backing store for the <see cref="InnerTrackCornerRadius"/> bindable property</summary>
	public static readonly BindableProperty InnerTrackCornerRadiusProperty = BindableProperty.Create(nameof(InnerTrackCornerRadius), typeof(CornerRadius), typeof(RangeSlider), defaultValue: RangeSliderDefaults.InnerTrackCornerRadius);

	/// <summary>The backing store for the <see cref="OuterTrackColor"/> bindable property.</summary>
	public static readonly BindableProperty OuterTrackColorProperty = BindableProperty.Create(nameof(OuterTrackColor), typeof(Color), typeof(RangeSlider), defaultValue: RangeSliderDefaults.OuterTrackColor);

	/// <summary>The backing store for the <see cref="OuterTrackSize"/> bindable property.</summary>
	public static readonly BindableProperty OuterTrackSizeProperty = BindableProperty.Create(nameof(OuterTrackSize), typeof(double), typeof(RangeSlider), defaultValue: RangeSliderDefaults.OuterTrackSize);

	/// <summary>The backing store for the <see cref="OuterTrackCornerRadius"/> bindable property</summary>
	public static readonly BindableProperty OuterTrackCornerRadiusProperty = BindableProperty.Create(nameof(OuterTrackCornerRadius), typeof(CornerRadius), typeof(RangeSlider), defaultValue: RangeSliderDefaults.OuterTrackCornerRadius);

	/// <summary>The backing store for the <see cref="FocusMode"/> bindable property.</summary>
	public static readonly BindableProperty FocusModeProperty = BindableProperty.Create(nameof(FocusMode), typeof(RangeSliderFocusMode), typeof(RangeSlider), defaultValue: RangeSliderDefaults.FocusMode);

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

	bool isClampingEnabled = false;

	/// <summary>Initializes a new instance of the <see cref="RangeSlider"/> class.</summary>
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

	void FinalizeInitialization()
	{
		isClampingEnabled = true;
		CoerceValue(LowerValueProperty);
		CoerceValue(UpperValueProperty);
		lowerSlider.DragStarted += HandleLowerSliderDragStarted;
		lowerSlider.DragCompleted += HandleLowerSliderDragCompleted;
		lowerSlider.PropertyChanged += HandleLowerSliderPropertyChanged;
		lowerSlider.Focused += HandlerLowerSliderFocused;
		upperSlider.DragStarted += HandleUpperSliderDragStarted;
		upperSlider.DragCompleted += HandleUpperSliderDragCompleted;
		upperSlider.PropertyChanged += HandleUpperSliderPropertyChanged;
		upperSlider.Focused += HandlerUpperSliderFocused;
		UpdateSliderRanges();
		UpdateLowerSliderValue();
		UpdateUpperSliderValue();
		UpdateFocusedSliderLayout();
		UpdateOuterTrackLayout();
		UpdateInnerTrackLayout();
	}

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
		if (e.PropertyName == MinimumValueProperty.PropertyName
			|| e.PropertyName == MaximumValueProperty.PropertyName
			|| e.PropertyName == StepSizeProperty.PropertyName)
		{
			UpdateSliderRanges();
		}

		if (e.PropertyName == WidthProperty.PropertyName
			|| e.PropertyName == MinimumValueProperty.PropertyName
			|| e.PropertyName == MaximumValueProperty.PropertyName
			|| e.PropertyName == StepSizeProperty.PropertyName)
		{
			UpdateFocusedSliderLayout();
		}

		if (e.PropertyName == LowerValueProperty.PropertyName)
		{
			UpdateLowerSliderValue();
		}

		if (e.PropertyName == UpperValueProperty.PropertyName)
		{
			UpdateUpperSliderValue();
		}

		if (e.PropertyName == WidthProperty.PropertyName
			|| e.PropertyName == MinimumValueProperty.PropertyName
			|| e.PropertyName == MaximumValueProperty.PropertyName
			|| e.PropertyName == OuterTrackSizeProperty.PropertyName)
		{
			UpdateOuterTrackLayout();
		}

		if (e.PropertyName == WidthProperty.PropertyName
			|| e.PropertyName == MinimumValueProperty.PropertyName
			|| e.PropertyName == MaximumValueProperty.PropertyName
			|| e.PropertyName == LowerValueProperty.PropertyName
			|| e.PropertyName == UpperValueProperty.PropertyName
			|| e.PropertyName == InnerTrackSizeProperty.PropertyName)
		{
			UpdateInnerTrackLayout();
		}
	}

	void HandleLowerSliderDragStarted(object? sender, EventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Lower;
		UpdateFocusedSliderLayout();
	}

	void HandleLowerSliderDragCompleted(object? sender, EventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Default;
		UpdateFocusedSliderLayout();
	}

	void HandleLowerSliderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == Slider.ValueProperty.PropertyName)
		{
			LowerValue = (StepSize == 0.0 ? lowerSlider.Value : Math.Round(lowerSlider.Value)) * GetUnit() + MinimumValue;
			FocusMode = RangeSliderFocusMode.Lower;
			UpdateInnerTrackLayout();
		}
	}

	void HandlerLowerSliderFocused(object? sender, FocusEventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Lower;
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderDragStarted(object? sender, EventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Upper;
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderDragCompleted(object? sender, EventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Default;
		UpdateFocusedSliderLayout();
	}

	void HandleUpperSliderPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == Slider.ValueProperty.PropertyName)
		{
			UpperValue = (StepSize == 0.0 ? upperSlider.Value : Math.Round(upperSlider.Value)) * GetUnit() + MinimumValue;
			FocusMode = RangeSliderFocusMode.Upper;
			UpdateInnerTrackLayout();
		}
	}

	void HandlerUpperSliderFocused(object? sender, FocusEventArgs e)
	{
		FocusMode = RangeSliderFocusMode.Upper;
		UpdateFocusedSliderLayout();
	}

	void UpdateFocusedSliderLayout()
	{
		double unit = GetUnit();
		lowerSlider.Minimum = upperSlider.Minimum = 0;
		lowerSlider.Maximum = upperSlider.Maximum = (MaximumValue - MinimumValue) / unit;
		lowerSlider.Maximum = upperSlider.Minimum = FocusMode switch
		{
			RangeSliderFocusMode.Lower => upperSlider.Value,
			RangeSliderFocusMode.Upper => lowerSlider.Value,
			_ => (lowerSlider.Value + upperSlider.Value) / 2,
		};

		double range = upperSlider.Maximum - lowerSlider.Minimum;
		double trackWidth = Width - PlatformThumbSize;
		if (range != 0)
		{
			lowerSlider.WidthRequest = trackWidth * (lowerSlider.Maximum - lowerSlider.Minimum) / range + PlatformThumbSize;
			upperSlider.WidthRequest = trackWidth * (upperSlider.Maximum - upperSlider.Minimum) / range + PlatformThumbSize;
		}
	}

	double GetUnit()
	{
		double unit = StepSize == 0 ? 1 : Math.Abs(StepSize);
		return MinimumValue <= MaximumValue ? unit : -unit;
	}

	void UpdateSliderRanges()
	{
		double unit = GetUnit();
		lowerSlider.Minimum = 0;
		lowerSlider.Maximum = (MaximumValue - MinimumValue) / unit;
		upperSlider.Minimum = 0;
		upperSlider.Maximum = (MaximumValue - MinimumValue) / unit;
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
		innerTrack.TranslationX = (Width - PlatformThumbSize) * (lowerSlider.Value - lowerSlider.Minimum) / (upperSlider.Maximum - lowerSlider.Minimum) + PlatformThumbSize / 2 - InnerTrackSize / 2;
		innerTrack.WidthRequest = (Width - PlatformThumbSize) * (upperSlider.Value - lowerSlider.Value) / (upperSlider.Maximum - lowerSlider.Minimum) + InnerTrackSize;
	}

	/// <summary>Gets or sets the minimum value</summary>
	public double MinimumValue
	{
		get => (double)GetValue(MinimumValueProperty);
		set => SetValue(MinimumValueProperty, value);
	}

	/// <summary>Gets or sets the maximum value</summary>
	public double MaximumValue
	{
		get => (double)GetValue(MaximumValueProperty);
		set => SetValue(MaximumValueProperty, value);
	}

	/// <summary>Gets or sets the lower value</summary>
	public double LowerValue
	{
		get => (double)GetValue(LowerValueProperty);
		set => SetValue(LowerValueProperty, value);
	}

	/// <summary>Gets or sets the upper value</summary>
	public double UpperValue
	{
		get => (double)GetValue(UpperValueProperty);
		set => SetValue(UpperValueProperty, value);
	}

	/// <summary>Gets or sets the step size</summary>
	public double StepSize
	{
		get => (double)GetValue(StepSizeProperty);
		set => SetValue(StepSizeProperty, value);
	}

	/// <summary>Gets or sets the lower thumb color</summary>
	public Color LowerThumbColor
	{
		get => (Color)GetValue(LowerThumbColorProperty);
		set => SetValue(LowerThumbColorProperty, value);
	}

	/// <summary>Gets or sets the upper thumb color</summary>
	public Color UpperThumbColor
	{
		get => (Color)GetValue(UpperThumbColorProperty);
		set => SetValue(UpperThumbColorProperty, value);
	}

	/// <summary>Gets or sets the inner track color</summary>
	public Color InnerTrackColor
	{
		get => (Color)GetValue(InnerTrackColorProperty);
		set => SetValue(InnerTrackColorProperty, value);
	}

	/// <summary>Gets or sets the inner track size</summary>
	public double InnerTrackSize
	{
		get => (double)GetValue(InnerTrackSizeProperty);
		set => SetValue(InnerTrackSizeProperty, value);
	}

	/// <summary>Gets or sets the inner track corner radius</summary>
	public CornerRadius InnerTrackCornerRadius
	{
		get => (CornerRadius)GetValue(InnerTrackCornerRadiusProperty);
		set => SetValue(InnerTrackCornerRadiusProperty, value);
	}
	/// <summary>Gets or sets the outer track color</summary>
	public Color OuterTrackColor
	{
		get => (Color)GetValue(OuterTrackColorProperty);
		set => SetValue(OuterTrackColorProperty, value);
	}

	/// <summary>Gets or sets the outer track size</summary>
	public double OuterTrackSize
	{
		get => (double)GetValue(OuterTrackSizeProperty);
		set => SetValue(OuterTrackSizeProperty, value);
	}

	/// <summary>Gets or sets the outer track corner radius</summary>
	public CornerRadius OuterTrackCornerRadius
	{
		get => (CornerRadius)GetValue(OuterTrackCornerRadiusProperty);
		set => SetValue(OuterTrackCornerRadiusProperty, value);
	}

	/// <summary>Gets the focus mode</summary>
	public RangeSliderFocusMode FocusMode
	{
		get => (RangeSliderFocusMode)GetValue(FocusModeProperty);
		private set => SetValue(FocusModeProperty, value);
	}

	/// <summary>Gets the platform-specific thumb size</summary>
#if WINDOWS
	public const double PlatformThumbSize = 17.5;
#else
	public const double PlatformThumbSize = 31.5;
#endif
}
