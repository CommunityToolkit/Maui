using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts embedded image resource ID to it ImageSource.
/// </summary>
public class ImageResourceConverter : BaseConverterOneWay<string?, ImageSource?>
{
	/// <inheritdoc/>
	public override ImageSource? DefaultConvertReturnValue { get; set; } = null;

	/// <summary>
	/// Converts embedded image resource ID to it ImageSource.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="culture">(Not Used)</param>
	/// <returns>The ImageSource related to the provided resource ID of the embedded image. If it's null it will returns null.</returns>
	[return: NotNullIfNotNull("value")]
	public override ImageSource? ConvertFrom(string? value, CultureInfo? culture = null) => value switch
	{
		null => null,
		_ => ImageSource.FromResource(value, Application.Current?.GetType().Assembly)
	};
}