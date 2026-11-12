using System.ComponentModel;
using System.Globalization;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Converters;

/// <summary>
/// A <see cref="TypeConverter"/> specific to converting a string value to a <see cref="FileMediaSource"/>.
/// </summary>
[TypeConverter(typeof(FileMediaSource))]
public sealed class FileMediaSourceConverter : TypeConverter
{
	/// <inheritdoc/>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	/// <exception cref="InvalidOperationException">Thrown when <paramref name="value"/> is empty or white space.</exception>
	public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		ArgumentNullException.ThrowIfNull(value);

		var filePath = value.ToString() ?? string.Empty;

		return string.IsNullOrWhiteSpace(filePath)
			? throw new InvalidOperationException($"Cannot convert \"{value}\" into {typeof(FileMediaSource)}")
			: (FileMediaSource)MediaSource.FromFile(filePath);
	}
}