using System.ComponentModel;
using System.Globalization;

namespace CommunityToolkit.Maui.MediaElement.Converters;

public sealed class MediaSourceConverter : TypeConverter
{
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
			=> sourceType == typeof(string);

	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
		=> destinationType == typeof(string);

	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		var valueAsString = value.ToString();

		if (string.IsNullOrWhiteSpace(valueAsString))
		{
			return null;
		}

		return Uri.TryCreate(valueAsString, UriKind.Absolute, out var uri) && uri.Scheme != "file"
			? MediaSource.FromUri(uri)
			: MediaSource.FromFile(valueAsString);
	}

	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
	{
		if (value is MediaSource valueAsMediaSource)
		{
			if (valueAsMediaSource is FileMediaSource fileMediaSource)
			{
				return fileMediaSource.ToString();
			}
			else if (valueAsMediaSource is UriMediaSource uriMediaSource)
			{
				return uriMediaSource.ToString();
			}

			// Probably a StreamMediaSource, can't translate that to a string
			return string.Empty;
		}

		// Value is not a MediaSource, return empty string
		return string.Empty;
	}
}
