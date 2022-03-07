using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Application = Microsoft.Maui.Controls.Application;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts embedded image resource ID to it ImageSource.
/// </summary>
public class ImageResourceConverter : BaseConverterOneWay<string?, ImageSource?>
{
	/// <summary>
	/// Converts embedded image resource ID to it ImageSource.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <returns>The ImageSource related to the provided resource ID of the embedded image. If it's null it will returns null.</returns>
	[return: NotNullIfNotNull("value")]
	public override ImageSource? ConvertFrom(string? value)
	{
		if (value == null)
		{
			return null;
		}

		if (value is not string imageId)
		{
			throw new ArgumentException("Value is not a string", nameof(value));
		}

		return ImageSource.FromResource(imageId, Application.Current?.GetType()?.Assembly);
	}
}