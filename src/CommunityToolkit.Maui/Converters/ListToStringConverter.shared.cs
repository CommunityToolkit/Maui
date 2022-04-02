﻿using System.Collections;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Concatenates the members of a collection, using the specified separator between each member.
/// </summary>
public class ListToStringConverter : BaseConverterOneWay<IEnumerable, string>
{
	string separator = string.Empty;

	/// <summary>
	/// The value that separates each item in the collection
	/// This value is superseded by the ConverterParameter, if provided
	/// If ConverterParameter is null, this Separator property will be used for the <see cref="ListToStringConverter"/>
	/// </summary>
	public string Separator
	{
		get => separator;
		set => separator = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Concatenates the items of a collection, using the specified <see cref="Separator"/> between each item. On each item ToString() will be called.
	/// </summary>
	/// <param name="value">The collection to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">The separator that should be between each collection item. This overrides the value in <see cref="Separator"/>.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>Concatenated members string separated by <see cref="Separator"/> or, if set, <paramref name="parameter"/>.</returns>
	public override string ConvertFrom(IEnumerable value, Type targetType, object? parameter, CultureInfo? culture)
	{
		ArgumentNullException.ThrowIfNull(value);

		parameter ??= Separator; // if the ConverterParameter is not assigned (aka parameter is null), we will default to the Separator property. 

		if (parameter is not string separator)
		{
			throw new ArgumentException("Parameter cannot be casted to string", nameof(parameter));
		}

		var collection = value
			.OfType<object>()
			.Select(x => x.ToString())
			.Where(x => !string.IsNullOrWhiteSpace(x));

		return string.Join(separator, collection);
	}
}