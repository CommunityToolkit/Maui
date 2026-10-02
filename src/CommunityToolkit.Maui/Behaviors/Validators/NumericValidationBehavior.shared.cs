using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Core;

namespace CommunityToolkit.Maui.Behaviors;

/// <summary>
/// The <see cref="NumericValidationBehavior"/> is a behavior that allows the user to determine if text input is a valid numeric value. For example, an <see cref="Entry"/> control can be styled differently depending on whether a valid or an invalid numeric input is provided. Additional properties handling validation are inherited from <see cref="ValidationBehavior"/>.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
[RequiresUnreferencedCode($"{nameof(NumericValidationBehavior)} is not trim safe because it uses bindings with string paths.")]
public partial class NumericValidationBehavior : ValidationBehavior<string>
{
	/// <summary>
	/// The minimum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial double MinimumValue { get; set; } = NumericValidationBehaviorDefaults.MinimumValue;

	/// <summary>
	/// The maximum numeric value that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial double MaximumValue { get; set; } = NumericValidationBehaviorDefaults.MaximumValue;

	/// <summary>
	/// The minimum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial int MinimumDecimalPlaces { get; set; } = NumericValidationBehaviorDefaults.MinimumDecimalPlaces;

	/// <summary>
	/// The maximum number of decimal places that will be allowed. This is a bindable property.
	/// </summary>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged))]
	public partial int MaximumDecimalPlaces { get; set; } = NumericValidationBehaviorDefaults.MaximumDecimalPlaces;

	/// <summary>
	/// The interval value that the entered value must be divisible by. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// A <see langword="null"/> value or a value of <c>0</c> disables interval validation (all values are considered valid). Value must be a finite number; non-finite values such as <see cref="double.NaN"/>, <see cref="double.PositiveInfinity"/> or <see cref="double.NegativeInfinity"/> are rejected. Negative values use their absolute magnitude. Divisibility accounts for floating-point rounding.
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged), ValidateValueMethodName = nameof(ValidateInterval))]
	public partial double? Interval { get; set; }

	static bool ValidateInterval(BindableObject bindableObject, object value)
		=> value switch
		{
			null => true,
			double interval => double.IsFinite(interval),
			_ => false
		};

	/// <inheritdoc/>
	protected override string? Decorate(string? value)
		=> base.Decorate(value)?.Trim();

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		if (!(double.TryParse(value, out var numeric)
			&& numeric >= MinimumValue
			&& numeric <= MaximumValue))
		{
			return new ValueTask<bool>(false);
		}

		// Interval of 0 would make every value invalid (x % 0 == NaN), so treat it as "no restriction"
		if (Interval.HasValue && Interval.Value != 0D)
		{
			// Interval validation relies on Interval being a finite double, as double.NaN and infinity
			// are already rejected by ValidateInterval during property assignment.

			var remainder = Math.Abs(numeric % Interval.Value);
			// Check if the remainder is close to 0 or close to the divisor (to account for floating-point inaccuracies)
			const double epsilon = 1e-9;
			if (remainder > epsilon && Math.Abs(remainder - Math.Abs(Interval.Value)) > epsilon)
			{
				return new ValueTask<bool>(false);
			}
		}

		var decimalDelimiterIndex = value.IndexOf(CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, StringComparison.Ordinal);
		var hasDecimalDelimiter = decimalDelimiterIndex >= 0;

		// If MaximumDecimalPlaces equals zero, ".5" or "14." should be considered as invalid inputs.
		if (hasDecimalDelimiter && MaximumDecimalPlaces == 0)
		{
			return new ValueTask<bool>(false);
		}

		var decimalPlaces = hasDecimalDelimiter
			? value.Substring(decimalDelimiterIndex + 1).Length
			: 0;

		return new ValueTask<bool>(decimalPlaces >= MinimumDecimalPlaces && decimalPlaces <= MaximumDecimalPlaces);
	}
}