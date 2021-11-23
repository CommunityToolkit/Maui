using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts <see cref="double"/> to <see cref="int"/> and vice versa.
/// </summary>
[ContentProperty(nameof(Ratio))]
public class DoubleToIntConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Multiplier / Denominator (Equals 1 by default).
	/// </summary>
	public double Ratio { get; set; } = 1;

	/// <summary>
	/// Converts <see cref="double"/> to <see cref="int"/>.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Multiplier (Equals 1 by default).</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns><see cref="int"/> value.</returns>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> value is double result
			? (int)Math.Round(result * GetParameter(parameter))
			: throw new ArgumentException("Value is not a valid double", nameof(value));

	/// <summary>
	/// Converts back <see cref="int"/> to <see cref="double"/>.
	/// </summary>
	/// <param name="value">Integer value.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Denominator (Equals 1 by default).</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns><see cref="double"/> value.</returns>
	[return: NotNull]
	public object? ConvertBack([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> value is int result
			? result / GetParameter(parameter)
			: throw new ArgumentException("Value is not a valid integer", nameof(value));

	double GetParameter(object? parameter) => parameter switch
	{
		null => Ratio,
		double d => d,
		int i => i,
		string s => double.TryParse(s, out var result)
			? result
			: throw new ArgumentException("Cannot parse number from the string", nameof(parameter)),
		_ => 1,
	};
}