using System.Collections;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value to a <see cref="bool"/> indicating whether the value is not null and not empty.
/// </summary>
[AcceptEmptyServiceProvider]
public partial class IsListNotNullOrEmptyConverter : BaseConverterOneWay<IEnumerable?, bool>
{
	/// <inheritdoc/>
	public override bool DefaultConvertReturnValue { get; set; } = false;

	/// <summary>
	/// Converts the incoming value to a <see cref="bool"/> indicating whether the value is not null and not empty.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>Whether the list is not null or empty</returns>
	public override bool ConvertFrom(IEnumerable? value, CultureInfo? culture = null) =>
		!IsListNullOrEmptyConverter.IsListNullOrEmpty(value);
}