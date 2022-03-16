using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts an <see cref="int"/> to a corresponding <see cref="bool"/> and vice versa.
/// </summary>
public class IntToBoolConverter : BaseConverter<int, bool>
{
	/// <summary>
	/// Converts the incoming <see cref="int"> to a <see cref="bool"/> indicating whether or not the value is not equal to 0.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>`false` if the supplied `value` is equal to `0` and `true` otherwise.</returns>
	public override bool ConvertFrom(int value) => value != 0;

	/// <summary>
	/// Converts the incoming <see cref="bool"> to an <see cref="int"/> indicating whether or not the value is true.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>`1` if the supplied `value` is `true` and `0` otherwise.</returns>
	public abstract int ConvertBackTo(bool value) => value ? 1 : 0;
}