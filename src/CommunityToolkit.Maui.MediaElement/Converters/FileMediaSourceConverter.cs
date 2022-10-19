using System.ComponentModel;
using System.Globalization;

namespace CommunityToolkit.Maui.MediaElement.Converters;

[TypeConverter(typeof(FileMediaSource))]
public sealed class FileMediaSourceConverter : TypeConverter
{
	public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
	{
		return string.IsNullOrWhiteSpace(value.ToString())
			? (FileMediaSource)MediaSource.FromFile(value.ToString())
			: throw new InvalidOperationException($"Cannot convert \"{value}\" into {typeof(FileMediaSource)}");
	}
}