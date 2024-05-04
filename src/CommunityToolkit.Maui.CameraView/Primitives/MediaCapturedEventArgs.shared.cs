namespace CommunityToolkit.Maui.Core.Primitives;

public class MediaCapturedEventArgs(Stream stream) : EventArgs
{
    public Stream Media { get; } = stream;
}
