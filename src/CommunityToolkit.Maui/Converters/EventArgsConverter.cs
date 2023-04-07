using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts/Extracts the incoming value from <see cref="EventArgs"/> object and returns the entire EventArgs of <see cref="EventArgs"/> property from it.
/// </summary>
public class EventArgsConverter : BaseConverterOneWay<EventArgs?, object?>
{
	/// <inheritdoc/>
	public override object? DefaultConvertReturnValue { get; set; } = null;

	/// <summary>
	/// Converts/Extracts the incoming value from <see cref="EventArgs"/> object and returns the value of <see cref="EventArgs"/> property from it.
	/// </summary>
	/// <param name="value">The entire EventArgs.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="EventArgs"/> object from object of type <see cref="EventArgs"/>.</returns>
	[return: NotNullIfNotNull(nameof(value))]
	public override object? ConvertFrom(EventArgs? value, CultureInfo? culture = null) => value switch
	{
		null => null,
		_ => value
	};
}
