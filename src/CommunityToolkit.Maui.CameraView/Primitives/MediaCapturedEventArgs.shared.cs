namespace CommunityToolkit.Maui.Views;

public class MediaCapturedEventArgs(Stream stream) : EventArgs
{
	public Stream Media { get; } = stream;
}