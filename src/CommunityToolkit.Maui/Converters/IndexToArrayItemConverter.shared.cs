using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="int"/> index to corresponding array item and vice versa.
/// </summary>
public class IndexToArrayItemConverter : BaseConverter<int, object?, Array>
{
	/// <inheritdoc/>
	public override object? DefaultConvertReturnValue { get; set; } = null;

	/// <inheritdoc />
	public override int DefaultConvertBackReturnValue { get; set; } = 0;

	/// <summary>
	/// Converts an <see cref="int"/> index to corresponding array item.
	/// </summary>
	/// <param name="value">The index of items array.</param>
	/// <param name="parameter">The items array.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The item from the array that corresponds to passed index.</returns>
	public override object? ConvertFrom(int value, Array parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(parameter);

		if (value < 0 || value >= parameter.Length)
		{
			throw new ArgumentOutOfRangeException(nameof(value), "Index was out of range.");
		}

		return parameter.GetValue(value);
	}

	/// <summary>
	/// Converts back an array item to corresponding index of the item in the array.
	/// </summary>
	/// <param name="value">The item from the array.</param>
	/// <param name="parameter">The items array.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>The index of the item from the array.</returns>
	public override int ConvertBackTo(object? value, Array parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(parameter);

		for (var i = 0; i < parameter.Length; i++)
		{
			var item = parameter.GetValue(i);
			if ((item is not null && item.Equals(value)) || (item is null && value is null))
			{
				return i;
			}
		}

		throw new ArgumentException("Value does not exist in the array.", nameof(value));
	}
}