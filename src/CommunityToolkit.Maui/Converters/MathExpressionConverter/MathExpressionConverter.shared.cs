using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for Math expressions
/// </summary>
public class MathExpressionConverter : BaseConverterOneWay<double, double, string>
{
	/// <inheritdoc/>
	public override double DefaultConvertReturnValue { get; set; } = 0.0d;

	/// <summary>
	/// Calculate the incoming expression string with one variable.
	/// </summary>
	/// <param name="value">The variable X for an expression</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public override double ConvertFrom(double value, string parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(parameter);

		var mathExpression = new MathExpression(parameter, new[] { value });
		return mathExpression.Calculate();
	}
}