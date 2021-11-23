using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for Math expressions
/// </summary>
public class MathExpressionConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Calculate the incoming expression string with one variable.
	/// </summary>
	/// <param name="value">The variable X for an expression</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public object? Convert(object? value, Type? targetType, [NotNull] object? parameter, CultureInfo? culture)
	{
		if (parameter is not string expression)
			throw new ArgumentException("The parameter should be of type String");

		if (value == null || !double.TryParse(value.ToString(), out var xValue))
			return null;

		var math = new MathExpression(expression, new[] { xValue });

		var result = math.Calculate();
		return result;
	}

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		=> throw new NotImplementedException();
}