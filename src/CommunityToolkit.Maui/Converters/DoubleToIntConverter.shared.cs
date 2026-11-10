using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts <see cref="double"/> to <see cref="int"/> and vice versa.
/// </summary>
[ContentProperty(nameof(Ratio)), AcceptEmptyServiceProvider]
public partial class DoubleToIntConverter : BaseConverter<double, int, object?>
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
	/// <param name="culture">The culture used to parse a <see cref="string"/> <paramref name="parameter"/> when it is not a number in the invariant format (for example "2,5" in a culture that uses a comma as decimal separator). When <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
	/// <returns><see cref="int"/> value.</returns>
	public override int ConvertFrom(double value, object? parameter = null, CultureInfo? culture = null)
		=> (int)Math.Round(value * GetParameter(parameter, culture));

	/// <summary>
	/// Converts back <see cref="int"/> to <see cref="double"/>.
	/// </summary>
	/// <param name="value">Integer value.</param>
	/// <param name="parameter">Denominator (Equals 1 by default).</param>
	/// <param name="culture">The culture used to parse a <see cref="string"/> <paramref name="parameter"/> when it is not a number in the invariant format (for example "2,5" in a culture that uses a comma as decimal separator). When <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
	/// <returns><see cref="double"/> value.</returns>
	public override double ConvertBackTo(int value, object? parameter = null, CultureInfo? culture = null)
		=> value / GetParameter(parameter, culture);

	// Invariant first so a XAML literal such as "2.5" means the same number for every user, like any other XAML numeric literal; the culture is only a fallback for locale-formatted input such as "2,5".
	static bool TryParseNumber(string text, CultureInfo? culture, out double result)
		=> double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result)
		|| double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, culture ?? CultureInfo.CurrentCulture, out result);

	double GetParameter(object? parameter, CultureInfo? culture) => parameter switch
	{
		null => Ratio,
		double d => d,
		int i => i,
		string s => TryParseNumber(s, culture, out var result) ? result : throw new ArgumentException("Cannot parse number from the string.", nameof(parameter)),
		_ => throw new ArgumentException("Parameter must be a valid number.")
	};
}
