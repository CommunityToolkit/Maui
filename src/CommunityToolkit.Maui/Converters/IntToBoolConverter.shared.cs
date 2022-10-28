using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="int"/> to a corresponding <see cref="bool"/> and vice versa.
/// </summary>
public class IntToBoolConverter : BaseConverter<int, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <inheritdoc />
	public override int DefaultConvertBackReturnValue { get; set; } = 0;

	/// <summary>
	/// Converts the incoming <see cref="int"/> to a <see cref="bool"/> indicating whether or not the value is not equal to 0.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns><c>false</c> if the supplied <paramref name="value"/> is equal to <c>0</c> and <c>true</c> otherwise.</returns>
	public override bool ConvertFrom(int value, CultureInfo? culture = null) => value is not 0;

	/// <summary>
	/// Converts the incoming <see cref="bool"/> to an <see cref="int"/> indicating whether or not the value is true.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns><c>1</c> if the supplied <paramref name="value"/> is <c>true</c> and <c>0</c> otherwise.</returns>
	public override int ConvertBackTo(bool value, CultureInfo? culture = null) => value ? 1 : 0;
}