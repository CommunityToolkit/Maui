using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// Converts the incoming value from <see cref="byte"/>[] and returns the object of a type <see cref="ImageSource"/> or vice versa.
/// </summary>
public class ByteArrayToImageSourceConverter : BaseConverter<byte[]?, ImageSource?>
{
	/// <summary>
	/// Converts the incoming value from <see cref="byte"/>[] and returns the object of a type <see cref="ImageSource"/>.
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An object of type <see cref="ImageSource"/>.</returns>
	[return: NotNullIfNotNull("value")]
	public override ImageSource? ConvertFrom(byte[]? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (value is null)
		{
			return null;
		}

		return ImageSource.FromStream(() => new MemoryStream(value));
	}

	/// <summary>
	/// Converts the incoming value from <see cref="StreamImageSource"/> and returns a <see cref="byte"/>[].
	/// </summary>
	/// <param name="value">The value to convert.</param>
	/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
	/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
	/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
	/// <returns>An object of type <see cref="ImageSource"/>.</returns>
	public override byte[]? ConvertBackTo(ImageSource? value, Type targetType, object? parameter, CultureInfo? culture)
	{
		if (value is null)
		{
			return null;
		}

		if (value is not StreamImageSource streamImageSource)
		{
			throw new ArgumentException("Expected value to be of type StreamImageSource.", nameof(value));
		}

		var streamFromImageSource = streamImageSource.Stream(CancellationToken.None).GetAwaiter().GetResult();

		if (streamFromImageSource is null)
		{
			return null;
		}

		using var memoryStream = new MemoryStream();
		streamFromImageSource.CopyTo(memoryStream);

		return memoryStream.ToArray();
	}
}