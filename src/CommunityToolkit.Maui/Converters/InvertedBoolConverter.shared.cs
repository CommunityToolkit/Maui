using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts true to false and false to true. Simple as that!
/// </summary>
public class InvertedBoolConverter : BaseConverter<bool, bool>
{
	/// <summary>
	/// Converts a <see cref="bool"/> to its inverse value.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An inverted <see cref="bool"/> from the one coming in.</returns>
	public override bool ConvertFrom(bool value, Type targetType, object? parameter, CultureInfo? culture) => !value;

	/// <summary>
	/// Converts a <see cref="bool"/> to its inverse value.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An inverted <see cref="bool"/> from the one coming in.</returns>
	public override bool ConvertBackTo(bool value, Type targetType, object? parameter, CultureInfo? culture) => !value;
}