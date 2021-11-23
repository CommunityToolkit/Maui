using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts boolean to object and vice versa.
/// </summary>
public class BoolToObjectConverter : BoolToObjectConverter<object>
{
}

/// <summary>
/// Converts <see cref="bool"/> to object and vice versa.
/// </summary>
public class BoolToObjectConverter<TObject> : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	/// The object that corresponds to True value.
	/// </summary>
	public TObject? TrueObject { get; set; }

	/// <summary>
	/// The object that corresponds to False value.
	/// </summary>
	public TObject? FalseObject { get; set; }

	/// <summary>
	/// Converts <see cref="bool"/> to object.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>The object assigned to <see cref="TrueObject"/> if value equals True, otherwise the value assigned to <see cref="FalseObject"/>.</returns>
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is bool result)
			return result ? TrueObject : FalseObject;

		throw new ArgumentException("Value is not a valid boolean", nameof(value));
	}

	/// <summary>
	/// Converts back object to <see cref="bool"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter.  This is not implemented.</param>
	/// <returns>True if value equals <see cref="TrueObject"/>, otherwise False.</returns>
	[return: NotNull]
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is TObject result)
			return result.Equals(TrueObject);

		if (default(TObject) == null && value == null && TrueObject == null)
			return true;

		throw new ArgumentException($"Value is not a valid {typeof(TObject).Name}", nameof(value));
	}
}