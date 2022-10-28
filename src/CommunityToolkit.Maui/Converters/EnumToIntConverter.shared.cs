using System.ComponentModel;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
///     Converts an <see cref="Enum" /> to its underlying <see cref="int" /> value.
/// </summary>
public class EnumToIntConverter : BaseConverter<Enum, int, Type>
{
	/// <inheritdoc/>
	public override int DefaultConvertReturnValue { get; set; } = 0;

	/// <inheritdoc/>
	public override Enum DefaultConvertBackReturnValue { get; set; } = DefaultEnum.Value;

	enum DefaultEnum { Value }

	/// <summary>
	/// Convert a default <see cref="Enum"/> (i.e., extending <see cref="int"/>) to corresponding underlying <see cref="int"/>
	/// </summary>
	/// <param name="value"><see cref="Enum"/> value to convert</param>
	/// <param name="parameter"></param>
	/// <param name="culture">Unused: Culture to use in the converter</param>
	/// <returns>The underlying <see cref="int"/> value of the passed enum value</returns>
	/// <exception cref="ArgumentException">If value is not an enumeration type</exception>
	public override int ConvertFrom(Enum value, Type? parameter = null, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);

		return Convert.ToInt32(value);
	}

	/// <summary>
	/// Returns the <see cref="Enum"/> associated with the specified <see cref="int"/> value defined in the targetType
	/// </summary>
	/// <param name="value"><see cref="Enum"/> value to convert</param>
	/// <param name="parameter"></param>
	/// <param name="culture">Unused: Culture to use in the converter</param>
	/// <returns>The underlying <see cref="Enum"/> of the associated targetType</returns>
	/// <exception cref="ArgumentException">If value is not a valid value in the targetType enum</exception>
	public override Enum ConvertBackTo(int value, Type parameter, CultureInfo? culture = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		ArgumentNullException.ThrowIfNull(parameter);

		if (!Enum.IsDefined(parameter, value))
		{
			throw new InvalidEnumArgumentException($"{value} is not valid for {parameter.Name}.");
		}

		return (Enum)Enum.ToObject(parameter, value);
	}
}