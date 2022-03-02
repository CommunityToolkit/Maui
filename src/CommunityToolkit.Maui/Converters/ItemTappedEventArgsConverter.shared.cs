
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts/Extracts the incoming value from <see cref="ItemTappedEventArgs"/> object and returns the value of <see cref="ItemTappedEventArgs.Item"/> property from it.
/// </summary>
public class ItemTappedEventArgsConverter : BaseConverterOneWay<ItemTappedEventArgs?, object?>
{
	/// <summary>
	/// Converts/Extracts the incoming value from <see cref="ItemTappedEventArgs"/> object and returns the value of <see cref="ItemTappedEventArgs.Item"/> property from it.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>A <see cref="ItemTappedEventArgs.Item"/> object from object of type <see cref="ItemTappedEventArgs"/>.</returns>
	[return: NotNullIfNotNull("value")]
	public override object? ConvertFrom(ItemTappedEventArgs? value) => value switch
	{
		null => null,
		_ => value.Item
	};
}