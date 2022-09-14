using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts/Extracts the incoming value from <see cref="SelectedItemChangedEventArgs"/> object and returns the value of <see cref="SelectedItemChangedEventArgs.SelectedItem"/> property from it.
/// </summary>
public class SelectedItemEventArgsConverter : BaseConverterOneWay<SelectedItemChangedEventArgs?, object?>
{
	/// <inheritdoc/>
	public override object? DefaultConvertReturnValue { get; set; } = null;

	/// <summary>
	/// Converts/Extracts the incoming value from <see cref="SelectedItemChangedEventArgs"/> object and returns the value of <see cref="SelectedItemChangedEventArgs.SelectedItem"/> property from it.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>A <see cref="SelectedItemChangedEventArgs.SelectedItem"/> object from object of type <see cref="SelectedItemChangedEventArgs"/>.</returns>
	[return: NotNullIfNotNull("value")]
	public override object? ConvertFrom(SelectedItemChangedEventArgs? value, CultureInfo? culture = null) => value switch
	{
		null => null,
		_ => value.SelectedItem
	};
}