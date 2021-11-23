using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts text (string, char) to certain case as specified with <see cref="Type"/> or the parameter of the Convert method.
/// </summary>
[ContentProperty(nameof(Type))]
public class TextCaseConverter : ValueConverterExtension, ICommunityToolkitValueConverter
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
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		var str = value?.ToString();
		if (str == null || string.IsNullOrWhiteSpace(str))
			return str;

		return GetParameter(parameter) switch
		{
			TextCaseType.Lower => str.ToLowerInvariant(),
			TextCaseType.Upper => str.ToUpperInvariant(),
			TextCaseType.FirstUpperRestLower => str.Substring(0, 1).ToUpperInvariant() + str.ToString().Substring(1).ToLowerInvariant(),
			_ => str
		};
	}

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) => throw new NotImplementedException();

	TextCaseType GetParameter(object? parameter) => parameter switch
	{
		TextCaseType type => type,
		string typeString => Enum.TryParse(typeString, out TextCaseType result) ? result : throw new ArgumentException("Cannot parse text case from the string", nameof(parameter)),
		int typeInt => Enum.IsDefined(typeof(TextCaseType), typeInt) ? (TextCaseType)typeInt : throw new ArgumentException("Cannot convert integer to text case enum value", nameof(parameter)),
		_ => Type,
	};
}