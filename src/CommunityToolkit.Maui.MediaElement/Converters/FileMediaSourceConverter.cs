using System.ComponentModel;

namespace CommunityToolkit.Maui.MediaElement.Converters;

//[TypeConversion(typeof(FileMediaSource))]
public sealed class FileMediaSourceConverter : TypeConverter
{
	// TODO
	//public override object ConvertFromInvariantString(string value)
	//{
	//	return value != null
	//		? (FileMediaSource)MediaSource.FromFile(value)
	//		: throw new InvalidOperationException($"Cannot convert \"{value}\" into {typeof(FileMediaSource)}");
	//}
}

