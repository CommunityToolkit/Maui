#nullable enable
using CommunityToolkit.Maui.Extensions.Internals;
using Microsoft.Maui.Controls;
using System.Globalization;

namespace CommunityToolkit.Maui.Converters
{
    /// <summary>
    /// Converts the incoming value from <see cref="byte"/>[] and returns the object of a type <see cref="ImageSource"/> or vice versa.
    /// </summary>
    public class ByteArrayToImageSourceConverter : ValueConverterExtension, IValueConverter
	{
		/// <summary>
		/// Converts the incoming value from <see cref="byte"/>[] and returns the object of a type <see cref="ImageSource"/>.
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
		/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
		/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
		/// <returns>An object of type <see cref="ImageSource"/>.</returns>
		public object? Convert(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		{
			if (value == null)
				return null;

			if (value is byte[] imageBytes)
				return ImageSource.FromStream(() => new MemoryStream(imageBytes));

			throw new ArgumentException("Expected value to be of type byte[].", nameof(value));
		}

		/// <summary>
		/// Converts the incoming value from <see cref="StreamImageSource"/> and returns a <see cref="byte"/>[].
		/// </summary>
		/// <param name="value">The value to convert.</param>
		/// <param name="targetType">The type of the binding target property. This is not implemented.</param>
		/// <param name="parameter">Additional parameter for the converter to handle. This is not implemented.</param>
		/// <param name="culture">The culture to use in the converter. This is not implemented.</param>
		/// <returns>An object of type <see cref="ImageSource"/>.</returns>
		public object? ConvertBack(object? value, Type? targetType, object? parameter, CultureInfo? culture)
		{
			if (value == null)
				return null;

			if (value is StreamImageSource streamImageSource)
			{
				var streamFromImageSource = streamImageSource.Stream(CancellationToken.None).Result;

				if (streamFromImageSource == null)
					return null;

				using var memoryStream = new MemoryStream();
				streamFromImageSource.CopyTo(memoryStream);

				return memoryStream.ToArray();
			}

			throw new ArgumentException("Expected value to be of type StreamImageSource.", nameof(value));
		}
	}
}