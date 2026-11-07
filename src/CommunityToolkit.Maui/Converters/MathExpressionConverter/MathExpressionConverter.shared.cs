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
	/// <param name="culture">The culture used to convert string inputs to numbers and to format values for <c>str()</c> and <c>len()</c>. When <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used. The syntax of the expression itself, such as numeric literals, is always culture-invariant.</param>
	/// <returns>A <see cref="double"/> The result of calculating an expression.</returns>
	public override object? ConvertFrom(object? inputValue, string parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(parameter);

		return new MathExpression(parameter, [inputValue], culture).CalculateResult();
	}
}