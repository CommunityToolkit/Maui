using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
///     Converts an <see cref="Enum" /> to its underlying <see cref="int" /> value.
/// </summary>
public class EnumToIntConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// Convert a default <see cref="Enum"/> (i.e., extending <see cref="int"/>) to corresponding underlying <see cref="int"/>
	/// </summary>
	/// <param name="value"><see cref="Enum"/> value to convert</param>
	/// <param name="targetType">Unused: type of the binding target property</param>
	/// <param name="parameter">Unused: Additional parameter for converter</param>
	/// <param name="culture">Unused: Culture to use in the converter</param>
	/// <returns>The underlying <see cref="int"/> value of the passed enum value</returns>
	/// <exception cref="ArgumentException">If value is not an enumeration type</exception>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture) =>
		value is Enum ? System.Convert.ToInt32(value) : throw new ArgumentException($"{value?.GetType().Name} is not a valid enumeration type");

	/// <summary>
	/// Returns the <see cref="Enum"/> associated with the specified <see cref="int"/> value defined in the targetType
	/// </summary>
	/// <param name="value"><see cref="Enum"/> value to convert</param>
	/// <param name="targetType">The type of the binding target property. Expected to be an enum.</param>
	/// <param name="parameter">Unused: Additional parameter for converter</param>
	/// <param name="culture">Unused: Culture to use in the converter</param>
	/// <returns>The underlying <see cref="Enum"/> of the associated targetType</returns>
	/// <exception cref="ArgumentException">If value is not a valid value in the targetType enum</exception>
	[return: NotNull]
	public object? ConvertBack([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture) =>
		value is int enumIntVal && targetType is not null && Enum.IsDefined(targetType, enumIntVal) ? Enum.ToObject(targetType, enumIntVal) : throw new ArgumentException($"{value} is not valid for {targetType?.Name ?? "null"}");
}