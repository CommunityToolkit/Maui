using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converters for Math expressions
/// </summary>
[AcceptEmptyServiceProvider]
public partial class MathExpressionConverter : BaseConverterOneWay<object?, object?, string>
{
	/// <inheritdoc/>
	public override object? DefaultConvertReturnValue { get; set; } = 0.0d;

	/// <summary>
	/// Calculate the incoming expression string with one variable.
	/// </summary>
	/// <param name="inputValue">The variable X for an expression</param>
	/// <param name="parameter">The expression to calculate.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public override object? ConvertFrom(object? inputValue, string parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(parameter);

		return new MathExpression(parameter, [inputValue]).CalculateResult();
	}
}