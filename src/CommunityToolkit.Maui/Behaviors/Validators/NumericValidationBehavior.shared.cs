using System.Globalization;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="NumericValidationBehavior"/> is a behavior that allows the user to determine if text input is a valid numeric value. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid numeric input is provided. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
public class NumericValidationBehavior : ValidationBehavior<string>
{
	/// <summary>
	/// Backing BindableProperty for the <see cref="MinimumValue"/> property.
	/// </summary>
	public static readonly BindableProperty MinimumValueProperty =
		BindableProperty.Create(nameof(MinimumValue), typeof(double), typeof(NumericValidationBehavior), double.NegativeInfinity, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaximumValue"/> property.
	/// </summary>
	public static readonly BindableProperty MaximumValueProperty =
		BindableProperty.Create(nameof(MaximumValue), typeof(double), typeof(NumericValidationBehavior), double.PositiveInfinity, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MinimumDecimalPlaces"/> property.
	/// </summary>
	public static readonly BindableProperty MinimumDecimalPlacesProperty =
		BindableProperty.Create(nameof(MinimumDecimalPlaces), typeof(int), typeof(NumericValidationBehavior), 0, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// Backing BindableProperty for the <see cref="MaximumDecimalPlaces"/> property.
	/// </summary>
	public static readonly BindableProperty MaximumDecimalPlacesProperty =
		BindableProperty.Create(nameof(MaximumDecimalPlaces), typeof(int), typeof(NumericValidationBehavior), int.MaxValue, propertyChanged: OnValidationPropertyChanged);

	/// <summary>
	/// The minimum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	public double MinimumValue
	{
		get => (double)GetValue(MinimumValueProperty);
		set => SetValue(MinimumValueProperty, value);
	}

	/// <summary>
	/// The maximum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	public double MaximumValue
	{
		get => (double)GetValue(MaximumValueProperty);
		set => SetValue(MaximumValueProperty, value);
	}

	/// <summary>
	/// The minimum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	public int MinimumDecimalPlaces
	{
		get => (int)GetValue(MinimumDecimalPlacesProperty);
		set => SetValue(MinimumDecimalPlacesProperty, value);
	}

	/// <summary>
	/// The maximum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	public int MaximumDecimalPlaces
	{
		get => (int)GetValue(MaximumDecimalPlacesProperty);
		set => SetValue(MaximumDecimalPlacesProperty, value);
	}

	/// <inheritdoc/>
	protected override string? Decorate(string? value)
		=> base.Decorate(value)?.Trim();

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		ArgumentNullException.ThrowIfNull(value);

		if (!(double.TryParse(value, out var numeric)
				&& numeric >= MinimumValue
				&& numeric <= MaximumValue))
		{
			return new ValueTask<bool>(false);
		}

		var decimalDelimiterIndex = value.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
		var hasDecimalDelimiter = decimalDelimiterIndex >= 0;

		// If MaximumDecimalPlaces equals zero, ".5" or "14." should be considered as invalid inputs.
		if (hasDecimalDelimiter && MaximumDecimalPlaces == 0)
		{
			return new ValueTask<bool>(false);
		}

		var decimalPlaces = hasDecimalDelimiter
			? value.Substring(decimalDelimiterIndex + 1, value.Length - decimalDelimiterIndex - 1).Length
			: 0;

		return new ValueTask<bool>(decimalPlaces >= MinimumDecimalPlaces && decimalPlaces <= MaximumDecimalPlaces);
	}
}