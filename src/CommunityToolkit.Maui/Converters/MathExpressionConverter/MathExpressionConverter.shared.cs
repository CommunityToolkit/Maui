using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for Math expressions
/// </summary>
public class MathExpressionConverter : BaseConverterOneWay<double, double>
{
	/// <summary>
	/// Calculate the incoming expression string with one variable.
	/// </summary>
	/// <param name="value">The variable X for an expression</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public override double ConvertFrom(double value, Type targetType, [NotNull] object? parameter, CultureInfo? culture)
	{
		if (parameter is not string expression)
		{
			throw new ArgumentException("The parameter should be of type String");
		}

		var math = new MathExpression(expression, new[] { value });

		var result = math.Calculate();
		return result;
	}
}