﻿using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Concatenates the members of a collection, using the specified separator between each member.
/// </summary>
public class ListToStringConverter : ValueConverterExtension, IValueConverter
{
    /// <summary>
    /// The separator that should be between each item in the collection
    /// </summary>
    public string Separator { get; set; } = string.Empty;

    /// <summary>
    /// Concatenates the items of a collection, using the specified <see cref="Separator"/> between each item. On each item ToString() will be called.
    /// </summary>
    /// <param name="value">The collection to convert.</param>
    /// <param name="targetType">The type of the binding target property. This is not implemented.</param>
    /// <param name="parameter">The separator that should be between each collection item. This overrides the value in <see cref="Separator"/>.</param>
    /// <param name="culture">The culture to use in the converter. This is not implemented.</param>
    /// <returns>Concatenated members string separated by <see cref="Separator"/> or, if set, <paramref name="parameter"/>.</returns>
    public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
    {
        if (value == null)
            return string.Empty;

        if (value is not IEnumerable enumerable)
            throw new ArgumentException("Value cannot be casted to IEnumerable", nameof(value));

        if ((parameter ?? Separator ?? string.Empty) is not string separator)
            throw new ArgumentException("Parameter cannot be casted to string", nameof(parameter));

        var collection = enumerable
            .OfType<object>()
            .Select(x => x.ToString())
            .Where(x => !string.IsNullOrWhiteSpace(x));

        return string.Join(separator, collection);
    }

    /// <summary>
    /// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="value">N/A</param>
    /// <param name="targetType">N/A</param>
    /// <param name="parameter">N/A</param>
    /// <param name="culture">N/A</param>
    /// <returns>N/A</returns>
    public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
        => throw new NotImplementedException();
}
