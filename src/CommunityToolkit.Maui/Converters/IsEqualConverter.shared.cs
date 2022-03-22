﻿using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Checks whether the incoming value equals the provided parameter.
/// </summary>
public class IsEqualConverter : BaseConverterOneWay<object?, bool>
{
	/// <summary>
	/// Checks whether the incoming value doesn't equal the provided parameter.
	/// </summary>
	/// <param name="value">The first object to compare.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The second object to compare.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>True if <paramref name="value"/> and <paramref name="parameter"/> are equal, False if they are not equal.</returns>
	public override bool ConvertFrom(object? value, Type targetType, object? parameter, CultureInfo? culture) => IsEqual(value, parameter);

	internal static bool IsEqual(object? value, object? parameter) =>
		(value is not null && value.Equals(parameter)) || (value is null && parameter is null);
}