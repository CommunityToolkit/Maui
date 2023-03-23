using System.ComponentModel;
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
public class TextCaseConverter : BaseConverterOneWay<string?, string?, TextCaseType?>
{
	TextCaseType type = TextCaseType.None;

	/// <inheritdoc/>
	public override string? DefaultConvertReturnValue { get; set; } = null;

	/// <summary>
	/// The desired text case that the text should be converted to.
	/// </summary>
	public TextCaseType Type
	{
		get => type;
		set
		{
			if (!Enum.IsDefined(typeof(TextCaseType), value))
			{
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(TextCaseType));
			}

			type = value;
		}
	}

	/// <summary>
	/// Converts text (string, char) to certain case.
	/// </summary>
	/// <param name="value">The text to convert.</param>
	/// <param name="parameter">The desired text case that the text should be converted to. Must match <see cref="TextCaseType"/> enum value.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The converted text representation with the desired casing.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public override string? ConvertFrom(string? value, TextCaseType? parameter = null, CultureInfo? culture = null)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		if (parameter is not null)
		{
			if (!Enum.IsDefined(typeof(TextCaseType), parameter))
			{
				throw new InvalidEnumArgumentException(nameof(parameter), (int)parameter, typeof(TextCaseType));
			}
		}
		else
		{
			parameter = Type;
		}


		return parameter switch
		{
			TextCaseType.Lower => value.ToLowerInvariant(),
			TextCaseType.Upper => value.ToUpperInvariant(),
			TextCaseType.FirstUpperRestLower => value[..1].ToUpperInvariant() + value[1..].ToLowerInvariant(),
			TextCaseType.None => value,
			_ => throw new NotSupportedException()
		};
	}
}