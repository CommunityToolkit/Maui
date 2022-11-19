using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for multiple math expressions
/// </summary>
public class MultiMathExpressionConverter : MultiValueConverterExtension, ICommunityToolkitMultiValueConverter
{
	/// <summary>
	/// Calculate the incoming expression string with variables.
	/// </summary>
	/// <param name="values">The array of variables for an expression</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	[return: NotNullIfNotNull(nameof(values))]
	public object? Convert(object?[]? values, Type targetType, [NotNull] object? parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(targetType);
		ArgumentNullException.ThrowIfNull(parameter);

		if (parameter is not string expression)
		{
			throw new ArgumentException("The parameter should be of type String.");
		}

		if (values is null || values.Any(x => !double.TryParse(x?.ToString(), out _)))
		{
			return null;
		}

		var args = new List<double>();
		foreach (var value in values)
		{
			var valueString = value?.ToString() ?? throw new ArgumentException("Values cannot be null.");

			var xValue = double.Parse(valueString);
			args.Add(xValue);
		}

		var math = new MathExpression(expression, args);
		return math.Calculate();
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