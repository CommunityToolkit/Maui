using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
///     Convert an <see cref="Enum" /> to corresponding <see cref="bool" />
/// </summary>
public class EnumToBoolConverter : ValueConverterExtension, ICommunityToolkitValueConverter
{
	/// <summary>
	///     Enum values, that converts to <c>true</c> (optional)
	/// </summary>
	public IList<Enum> TrueValues { get; } = new List<Enum>();

	/// <summary>
	///     Convert an <see cref="Enum" /> to corresponding <see cref="bool" />
	/// </summary>
	/// <param name="value"><see cref="Enum" /> value to convert</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">
	///     Additional parameter for converter. Can be used for comparison instead of
	///     <see cref="TrueValues" />
	/// </param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>
	///     False, if the value is not in <see cref="TrueValues" />. False, if <see cref="TrueValues" /> is empty and
	///     value not equal to parameter.
	/// </returns>
	/// <exception cref="ArgumentException">If value is not an <see cref="Enum" /></exception>
	[return: NotNull]
	public object? Convert([NotNull] object? value, Type? targetType, object? parameter, CultureInfo? culture)
	{
		if (value is not Enum enumValue)
			throw new ArgumentException("The value should be of type Enum", nameof(value));

		return TrueValues.Count == 0
			? CompareTwoEnums(enumValue, parameter as Enum)
			: TrueValues.Any(item => CompareTwoEnums(enumValue, item));

		static bool CompareTwoEnums(Enum valueToCheck, object? referenceValue)
		{
			if (referenceValue is not Enum referenceEnumValue)
				return false;

			var valueToCheckType = valueToCheck.GetType();
			if (valueToCheckType != referenceEnumValue.GetType())
				return false;

			if (valueToCheckType.GetTypeInfo().GetCustomAttribute<FlagsAttribute>() != null)
				return referenceEnumValue.HasFlag(valueToCheck);

			return Equals(valueToCheck, referenceEnumValue);
		}
	}

	/// <summary>
	/// This method is not implemented and will throw a <see cref="NotImplementedException"/>.
	/// </summary>
	/// <param name="value">N/A</param>
	/// <param name="targetType">N/A</param>
	/// <param name="parameter">N/A</param>
	/// <param name="culture">N/A</param>
	/// <returns>N/A</returns>
	public object ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture) => throw new NotImplementedException();
}