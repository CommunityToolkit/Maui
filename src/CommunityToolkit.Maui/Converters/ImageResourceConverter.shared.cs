using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts embedded image resource ID to it ImageSource.
/// </summary>
public class ImageResourceConverter : ICommunityToolkitValueConverter
{
	/// <summary>
	/// Converts embedded image resource ID to it ImageSource.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The ImageSource related to the provided resource ID of the embedded image. If it's null it will returns null.</returns>
	[return: NotNullIfNotNull("value")]
	public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value == null)
			return null;

		if (value is not string imageId)
			throw new ArgumentException("Value is not a string", nameof(value));

		return ImageSource.FromResource(imageId, Application.Current?.GetType()?.Assembly);
	}

	/// <inheritdoc />
	public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) => throw new NotImplementedException();
}