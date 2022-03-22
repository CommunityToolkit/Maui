using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>Different types of conditions that can be used in <see cref="Converters.VariableMultiValueConverter"/>.</summary>
public enum MultiBindingCondition
{
	/// <summary>None of the values should be true.</summary>
	None,
	/// <summary>All of the values should be true.</summary>
	All,
	/// <summary>Any of the values should be true.</summary>
	Any,
	/// <summary>The exact number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
	Exact,
	/// <summary>At least the number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
	GreaterThan,
	/// <summary>At most the number as configured in <see cref="Converters.VariableMultiValueConverter.Count"/> should be true.</summary>
	LessThan
}

/// <summary>
/// The <see cref="VariableMultiValueConverter"/> is a converter that allows users to convert multiple <see cref="bool"/> value bindings to a single <see cref="bool"/>. It does this by enabling them to specify whether All, Any, None or a specific number of values are true as specified in <see cref="ConditionType"/>. This is useful when combined with the <see cref="MultiBinding"/>.
/// </summary>
public class VariableMultiValueConverter : MultiValueConverterExtension, ICommunityToolkitMultiValueConverter
{
	/// <summary>
	/// Indicates how many values should be true out of the provided boolean values in the <see cref="MultiBinding"/>. Supports the following values: All, None, Any, GreaterThan, LessThan.
	/// </summary>
	public MultiBindingCondition ConditionType { get; set; }

	/// <summary>
	/// The number of values that should be true when using <see cref="ConditionType"/> GreaterThan, LessThan or Exact.
	/// </summary>
	public int Count { get; set; }

	/// <summary>
	/// Converts multiple <see cref="bool"/> value bindings to a single <see cref="bool"/>. It does this by enabling them to specify whether All, Any, None or a specific number of values are true as specified in <see cref="ConditionType"/>. This is useful when combined with the <see cref="MultiBinding"/>.
	/// </summary>
	/// <param name="values">The values (of type <see cref="bool"/>) that should be converted.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>A single <see cref="bool"/> value dependant on the configuration for this converter.</returns>
	[return: NotNull]
	public object? Convert(object[]? values, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (values == null || values.Length == 0)
		{
			return false;
		}

		var boolValues = values.OfType<bool>().ToArray();

		if (boolValues.Length != values.Length)
		{
			return false;
		}

		var count = boolValues.Count(v => v);

		return ConditionType switch
		{
			MultiBindingCondition.Any => count >= 1,
			MultiBindingCondition.None => count == 0,
			MultiBindingCondition.Exact => count == Count,
			MultiBindingCondition.GreaterThan => count > Count,
			MultiBindingCondition.LessThan => count < Count,
			_ => count == boolValues.Count(),
		};
	}

	/// <summary>
	/// Converts one <see cref="bool"/> value. Returns null if <paramref name="value"/> is not a <see cref="bool"/> value or when not all <paramref name="targetTypes"/> can be assigned from <see cref="bool"/>.
	/// </summary>
	/// <param name="value">The <see cref="bool"/> value that should be converted.</param>
	/// <param name="targetTypes">The types of the binding target properties.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>All bindings that evaluate to true if <paramref name="value"/> is true. Or null if <paramref name="value"/> is not a <see cref="bool"/> value or <paramref name="value"/> is false.</returns>
	public object[]? ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo? culture)
	{
		if (value is not bool boolValue || targetTypes?.Any(t => !t.IsAssignableFrom(typeof(bool))) is true)
		{
			return null;
		}

		return boolValue ? targetTypes?.Select(t => ConditionType == MultiBindingCondition.All).OfType<object>().ToArray() : null;
	}
}