using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// The text cases that can be used with <see cref="TextCaseConverter"/> to convert text to a specific case.
/// </summary>
public enum TextCaseType
{
	/// <summary>Should not be converted</summary>
	None,

	/// <summary>Convert to uppercase</summary>
	Upper,

	/// <summary>Convert to lowercase</summary>
	Lower,

	/// <summary>Converts the first letter to upper only</summary>
	FirstUpperRestLower,
}

/// <summary>
/// Converts text (string, char) to certain case as specified with <see cref="Type"/> or the parameter of the Convert method.
/// </summary>
[ContentProperty(nameof(Type))]
public class TextCaseConverter : BaseConverterOneWay<string?, string?>
{
	/// <summary>
	/// The desired text case that the text should be converted to.
	/// </summary>
	public TextCaseType Type { get; set; }

	/// <summary>
	/// Converts text (string, char) to certain case.
	/// </summary>
	/// <param name="value">The text to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The desired text case that the text should be converted to. Must match <see cref="TextCaseType"/> enum value.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The converted text representation with the desired casing.</returns>
	[return: NotNullIfNotNull("value")]
	public override string? ConvertFrom(string? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		return GetParameter(parameter) switch
		{
			TextCaseType.Lower => value.ToLowerInvariant(),
			TextCaseType.Upper => value.ToUpperInvariant(),
			TextCaseType.FirstUpperRestLower => value.Substring(0, 1).ToUpperInvariant() + value.ToString().Substring(1).ToLowerInvariant(),
			TextCaseType.None => value,
			_ => throw new NotSupportedException()
		};
	}

	TextCaseType GetParameter(object? parameter) => parameter switch
	{
		TextCaseType type => type,
		string typeString => Enum.TryParse(typeString, out TextCaseType result) ? result : throw new ArgumentException("Cannot parse text case from the string", nameof(parameter)),
		int typeInt => Enum.IsDefined(typeof(TextCaseType), typeInt) ? (TextCaseType)typeInt : throw new ArgumentException("Cannot convert integer to text case enum value", nameof(parameter)),
		_ => Type,
	};
}