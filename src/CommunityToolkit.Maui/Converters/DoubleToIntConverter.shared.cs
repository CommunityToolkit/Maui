using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts <see cref="double"/> to <see cref="int"/> and vice versa.
/// </summary>
[ContentProperty(nameof(Ratio))]
public class DoubleToIntConverter : BaseConverter<double, int, object?>
{
	/// <inheritdoc/>
	public override int DefaultConvertReturnValue { get; set; } = 0;

	/// <inheritdoc/>
	public override double DefaultConvertBackReturnValue { get; set; } = 0.0d;

	/// <summary>
	/// Multiplier / Denominator (Equals 1 by default).
	/// </summary>
	public double Ratio { get; set; } = 1;

	/// <summary>
	/// Converts <see cref="double"/> to <see cref="int"/>.
	/// </summary>
	/// <param name="value"><see cref="double"/> value.</param>
	/// <param name="parameter">Multiplier (Equals 1 by default).</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns><see cref="int"/> value.</returns>
	public override int ConvertFrom(double value, object? parameter = null, CultureInfo? culture = null)
		=> (int)Math.Round(value * GetParameter(parameter));

	/// <summary>
	/// Converts back <see cref="int"/> to <see cref="double"/>.
	/// </summary>
	/// <param name="value">Integer value.</param>
	/// <param name="parameter">Denominator (Equals 1 by default).</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns><see cref="double"/> value.</returns>
	public override double ConvertBackTo(int value, object? parameter = null, CultureInfo? culture = null)
		=> value / GetParameter(parameter);

	double GetParameter(object? parameter) => parameter switch
	{
		null => Ratio,
		double d => d,
		int i => i,
		string s => double.TryParse(s, out var result) ? result : throw new ArgumentException("Cannot parse number from the string.", nameof(parameter)),
		_ => throw new ArgumentException("Parameter must be a valid number.")
	};
}