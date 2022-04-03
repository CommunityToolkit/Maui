using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Returns a string array that contains the substrings in this string that are delimited by <see cref="Separator"/>.
/// </summary>
public class StringToListConverter : BaseConverterOneWay<string?, IEnumerable<string>>
{
	string separator = " ";
	IList<string> separators = Array.Empty<string>();

	/// <summary>
	/// The string that delimits the substrings in this string
	/// This value is superseded by the ConverterParameter, if provided
	/// This value is also superseded by <see cref="Separators"/>, if provided
	/// If ConverterParameter is null and <see cref="Separators"/> is not assigned, this property will be used to delimit the substrings
	/// </summary>
	public string Separator
	{
		get => separator;
		set
		{
			if (value is null)
			{
				throw new ArgumentNullException(nameof(value));
			}

			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException("An empty string is not a vald separator", nameof(value));
			}

			separator = value;
		}
	}

	/// <summary>
	/// The strings that delimits the substrings in this string
	/// This value is superseded by the ConverterParameter, if provided
	/// If ConverterParameter is null, this property will be used to delimit the substrings
	/// </summary>
	public IList<string> Separators
	{
		get => separators;
		set
		{
			ArgumentNullException.ThrowIfNull(value);

			foreach (var stringValue in value)
			{
				if (stringValue is null)
				{
					throw new ArgumentNullException(nameof(value), $"{nameof(value)} cannot contain null strings");
				}
				else if (string.IsNullOrEmpty(stringValue))
				{
					throw new ArgumentException("An empty string is not a valid separator", nameof(value));
				}
			}

			separators = value;
		}
	}

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
	public override IEnumerable<string> ConvertFrom(string? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (value is null)
		{
			return Array.Empty<string>();
		}

		if (parameter is string[] separators)
		{
			foreach (var stringValue in separators)
			{
				if (stringValue is null)
				{
					throw new ArgumentNullException(nameof(value), $"{nameof(value)} cannot contain null strings");
				}
				else if (string.IsNullOrEmpty(stringValue))
				{
					throw new ArgumentException("An empty string is not a valid separator", nameof(value));
				}
			}

			return Split(value, separators);
		}
		else if (parameter is string separator)
		{
			if (string.IsNullOrEmpty(separator))
			{
				throw new ArgumentException("An empty string is not a vald separator", nameof(value));
			}

			return Split(value, separator);
		}
		else if (parameter is not null)
		{
			throw new ArgumentException("Parameter cannot be casted to string nor string[]", nameof(parameter));
		}

		if (Separators.Count > 1)
		{
			return Split(value, Separators.ToArray());
		}

		return Split(value, Separator);
	}

	string[] Split(string valueToSplit, params string[] separators) => valueToSplit.Split(separators, SplitOptions);
}