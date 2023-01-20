using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// A <see cref="TypeConverter"/> specific to converting a string value to a <see cref="MediaSource"/>.
/// </summary>
public sealed class MediaSourceConverter : TypeConverter
{
	const string embeddedResourcePrefix = "embed://";
	const string fileSystemPrefix = "filesystem://";

	/// <inheritdoc/>
	public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
			=> sourceType == typeof(string);

	/// <inheritdoc/>
	public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
		=> destinationType == typeof(string);

	/// <inheritdoc/>
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		var valueAsString = value?.ToString() ?? string.Empty;

		if (string.IsNullOrWhiteSpace(valueAsString))
		{
			return null;
		}

		var valueAsStringLowercase = valueAsString.ToLowerInvariant();

		if (valueAsStringLowercase.StartsWith(embeddedResourcePrefix))
		{
			return MediaSource.FromResource(
				valueAsString[embeddedResourcePrefix.Length..]);
		}
		else if (valueAsStringLowercase.StartsWith(fileSystemPrefix))
		{
			return MediaSource.FromFile(valueAsString[fileSystemPrefix.Length..]);
		}

		return Uri.TryCreate(valueAsString, UriKind.Absolute, out var uri) && uri.Scheme != "file"
			? MediaSource.FromUri(uri)
			: MediaSource.FromFile(valueAsString);
	}

	/// <inheritdoc/>
	public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType) => value switch
	{
		UriMediaSource uriMediaSource => uriMediaSource.ToString(),
		FileMediaSource fileMediaSource => fileMediaSource.ToString(),
		ResourceMediaSource resourceMediaSource => resourceMediaSource.ToString(),
		MediaSource => string.Empty,
		_ => throw new ArgumentException($"Invalid Media Source", nameof(value))
	};
}