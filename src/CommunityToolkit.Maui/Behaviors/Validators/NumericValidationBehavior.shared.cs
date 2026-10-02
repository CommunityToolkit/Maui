using System.Diagnostics;
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
	/// The interval that a valid numeric value must be a multiple of. For example, an <see cref="Interval"/> of 0.25 allows 1.5 and 1.75 but not 1.6.
	/// When <see langword="null"/> (the default), no interval restriction is applied; an <see cref="Interval"/> of 0 only allows 0. This is a bindable property.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Multiples are counted from zero in both directions (..., -10, -5, 0, 5, 10, ...), not from <see cref="MinimumValue"/>:
	/// with an <see cref="Interval"/> of 5 and a <see cref="MinimumValue"/> of 3, 5 and 10 are valid but 3 and 8 are not.
	/// <see cref="MinimumValue"/>, <see cref="MaximumValue"/>, <see cref="MinimumDecimalPlaces"/> and <see cref="MaximumDecimalPlaces"/> still apply.
	/// </para>
	/// <para>
	/// A negative <see cref="Interval"/> behaves like its absolute value.
	/// An <see cref="Interval"/> of 0 is not the same as <see langword="null"/>: 0 is the only multiple of 0, so only a value of 0 can be valid.
	/// </para>
	/// <para>
	/// Most decimal fractions cannot be stored exactly in a <see cref="double"/>, so a small rounding tolerance is applied: 0.3 counts as a multiple of 0.1.
	/// The tolerance assumes <see cref="Interval"/> is the <see cref="double"/> closest to the intended step, such as 0.1 written in code or XAML.
	/// An <see cref="Interval"/> that carries extra rounding error, e.g. 1.1 - 1.0 (0.10000000000000009) or the <see cref="float"/> 0.1f (0.10000000149011612), rejects 0.1 and 0.3, so round such a value first, e.g. with <see cref="Math.Round(double, int)"/>.
	/// </para>
	/// <para>
	/// Because a <see cref="double"/> holds only about 15 significant digits, a value within about one part in 10^15 of a multiple may be treated as a multiple:
	/// with an <see cref="Interval"/> of 0.3, 1000000000000000 is valid although the nearest multiple is 999999999999999.9.
	/// </para>
	/// <para>
	/// <see cref="Interval"/> must be <see langword="null"/> or a finite number. Assigning <see cref="double.NaN"/>, <see cref="double.PositiveInfinity"/> or <see cref="double.NegativeInfinity"/> does not throw;
	/// the assignment is ignored and the previous value is kept (.NET MAUI logs a warning when a logger is available).
	/// </para>
	/// </remarks>
	[BindableProperty(PropertyChangedMethodName = nameof(OnValidationPropertyChanged), ValidateValueMethodName = nameof(ValidateInterval))]
	public partial double? Interval { get; set; }

	/// <inheritdoc/>
	protected override string? Decorate(string? value)
		=> base.Decorate(value)?.Trim();

	/// <inheritdoc/>
	protected override ValueTask<bool> ValidateAsync(string? value, CancellationToken token)
	{
		if (value is null)
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: Value is invalid because it is null");
			return new ValueTask<bool>(false);
		}

		if (!double.TryParse(value, out var numeric))
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because it cannot be parsed as a number using the current culture ({CultureInfo.CurrentCulture.Name})");
			return new ValueTask<bool>(false);
		}

		if (double.IsNaN(numeric))
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because it parses to NaN (not a number)");
			return new ValueTask<bool>(false);
		}

		// Negated comparisons ensure that a NaN MinimumValue or MaximumValue makes every value invalid
		if (!(numeric >= MinimumValue))
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because {numeric} is less than {nameof(MinimumValue)} ({MinimumValue})");
			return new ValueTask<bool>(false);
		}

		if (!(numeric <= MaximumValue))
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because {numeric} is greater than {nameof(MaximumValue)} ({MaximumValue})");
			return new ValueTask<bool>(false);
		}

		if (Interval is double interval && !IsMultipleOf(numeric, interval))
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because {numeric} is not a multiple of {nameof(Interval)} ({interval})");
			return new ValueTask<bool>(false);
		}

		var decimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
		var decimalDelimiterIndex = value.IndexOf(decimalSeparator, StringComparison.Ordinal);
		var hasDecimalDelimiter = decimalDelimiterIndex >= 0;

		// If MaximumDecimalPlaces equals zero, ".5" or "14." should be considered as invalid inputs.
		if (hasDecimalDelimiter && MaximumDecimalPlaces == 0)
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because it contains a decimal separator (\"{decimalSeparator}\") and {nameof(MaximumDecimalPlaces)} is 0");
			return new ValueTask<bool>(false);
		}

		var decimalPlaces = hasDecimalDelimiter
			? value.Substring(decimalDelimiterIndex + 1).Length
			: 0;

		if (decimalPlaces < MinimumDecimalPlaces)
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because its number of decimal places ({decimalPlaces}) is less than {nameof(MinimumDecimalPlaces)} ({MinimumDecimalPlaces})");
			return new ValueTask<bool>(false);
		}

		if (decimalPlaces > MaximumDecimalPlaces)
		{
			Trace.WriteLine($"{nameof(NumericValidationBehavior)}: \"{value}\" is invalid because its number of decimal places ({decimalPlaces}) is greater than {nameof(MaximumDecimalPlaces)} ({MaximumDecimalPlaces})");
			return new ValueTask<bool>(false);
		}

		return new ValueTask<bool>(true);
	}

	static bool ValidateInterval(BindableObject bindableObject, object value) => value switch
	{
		null => true,
		double interval => double.IsFinite(interval),
		_ => false
	};

	static bool IsMultipleOf(double value, double interval)
	{
		// Infinity is not a multiple of any finite interval
		// Infinity passes the range check whenever the bound on its side is infinite (MaximumValue for +∞, MinimumValue for -∞; both are by default), e.g. the input "1e400", or "∞" in most cultures
		if (!double.IsFinite(value))
		{
			return false;
		}

		// 0 is the only multiple of 0
		// This must be checked explicitly because Math.IEEERemainder(value, 0) returns NaN
		if (interval == 0)
		{
			return value == 0;
		}

		// The magnitude of Math.IEEERemainder is the exact distance between value and its nearest multiple of interval
		// Most decimal fractions cannot be stored exactly in a double, so a decimal multiple can be a tiny distance away from a multiple, e.g. Math.IEEERemainder(0.3, 0.1) returns -2.7755575615628914E-17
		// Rounding the decimal value and interval to the nearest double shifts that distance by at most 2^-52 × |value| (for normal, non-subnormal doubles), so we tolerate twice that amount
		// Because the tolerance scales with value, large multiples (e.g. 1000000000 with an Interval of 0.1) are accepted while tiny intervals (e.g. 1E-10) are still enforced
		const double relativeTolerance = 4.440892098500626E-16; // 2^-51

		return Math.Abs(Math.IEEERemainder(value, interval)) <= Math.Abs(value) * relativeTolerance;
	}
}