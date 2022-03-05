using System.Collections;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
/// </summary>
public class IsListNullOrEmptyConverter : BaseConverterOneWay<IEnumerable?, bool>
{
	/// <summary>
	/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is null or empty.</returns>
	public override bool ConvertFrom(IEnumerable? value) => IsListNullOrEmpty(value);

	internal static bool IsListNullOrEmpty(IEnumerable? value)
	{
		if (value is null)
		{
			return true;
		}
		
		return !value.GetEnumerator().MoveNext();
	}
}