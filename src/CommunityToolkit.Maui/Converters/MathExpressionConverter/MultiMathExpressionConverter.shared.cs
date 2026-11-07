using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for multiple math expressions
/// </summary>
[AcceptEmptyServiceProvider]
public class MultiMathExpressionConverter : MultiValueConverterExtension, ICommunityToolkitMultiValueConverter
{
	/// <summary>
	/// Calculate the incoming expression string with variables.
	/// </summary>
	/// <param name="values">The array of variables for an expression</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture used to convert string values to numbers and to format values for <c>str()</c> and <c>len()</c>. When <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used. The syntax of the expression itself, such as numeric literals, is always culture-invariant, but quoted string literals in the expression are treated as values and are converted with this culture.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public object? Convert(object?[]? values, Type targetType, [NotNull] object? parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(targetType);
		ArgumentNullException.ThrowIfNull(parameter);

		if (parameter is not string expression)
		{
			throw new ArgumentException("The parameter should be of type String.");
		}

		return values is null
			? null
			: new MathExpression(expression, values, culture).CalculateResult();
	}

	/// <summary>
	/// This method is not supported and will throw a <see cref="NotSupportedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetTypes">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo? culture)
		=> throw new NotSupportedException("Impossible to revert to original value. Consider setting BindingMode to OneWay.");
}