using System.Collections;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
/// </summary>
public class IsListNullOrEmptyConverter : BaseConverterOneWay<IEnumerable?, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Converts the incoming value to a <see cref="bool"/> indicating whether or not the value is null or empty.
	/// </summary>
	/// <param name="value">IEnumerable to convert</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="bool"/> indicating if the incoming value is null or empty.</returns>
	public override bool ConvertFrom(IEnumerable? value, CultureInfo? culture = null) => IsListNullOrEmpty(value);

	internal static bool IsListNullOrEmpty(IEnumerable? value) => value switch
	{
		null => true,
		_ => !value.GetEnumerator().MoveNext()
	};
}