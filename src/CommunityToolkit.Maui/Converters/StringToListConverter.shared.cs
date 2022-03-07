using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Returns a string array that contains the substrings in this string that are delimited by <see cref="Separator"/>.
/// </summary>
public class StringToListConverter : BaseConverterOneWay
{
	/// <summary>
	/// The string that delimits the substrings in this string.
	/// </summary>
	public string Separator { get; set; } = string.Empty;

	/// <summary>
	/// The strings that delimits the substrings in this string.
	/// </summary>
	public IList<string> Separators { get; } = new List<string>();

	/// <summary>
	/// A bitwise combination of the enumeration values that specifies whether to trim substrings and include empty substrings.
	/// </summary>
	public StringSplitOptions SplitOptions { get; set; } = StringSplitOptions.None;

	/// <summary>
	/// Returns a string array that contains the substrings in this string that are delimited by <see cref="Separators"/>.
	/// </summary>
	/// <param name="value">The string to split.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The string or strings that delimits the substrings in this string. This overrides the value in <see cref="Separator"/> and <see cref="Separators"/>.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An array whose elements contain the substrings in this string that are delimited by <see cref="Separator"/> or, if set, <see cref="Separators"/> or, if set, <paramref name="parameter"/>.</returns>
	[return: NotNull]
	public override object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value == null)
		{
			return Enumerable.Empty<string>();
		}

		if (value is not string valueToSplit)
		{
			throw new ArgumentException("Value cannot be casted to string", nameof(value));
		}

		if (parameter is string[] separators)
		{
			return Split(valueToSplit, separators);
		}

		if (parameter is string separator)
		{
			return Split(valueToSplit, separator);
		}

		if (parameter != null)
		{
			throw new ArgumentException("Parameter cannot be casted to string nor string[]", nameof(parameter));
		}

		if (Separators.Count > 1)
		{
			return Split(valueToSplit, Separators.ToArray());
		}

		return Split(valueToSplit, Separator);
	}

	string[] Split(string valueToSplit, params string[] separators) => valueToSplit.Split(separators, SplitOptions);
}