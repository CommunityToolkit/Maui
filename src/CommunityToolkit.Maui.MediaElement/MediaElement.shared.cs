namespace CommunityToolkit.Maui.MediaElement;

public partial class MediaElement : View, IMediaElement
{
	public MediaElement()
	{
	}

	public MediaSource? Source { get; set; }

	public TimeSpan Position { get; set; }

	public TimeSpan Duration { get; }

	public void Pause() { }

	public void Play() { }

	public void Stop() { }

#if (NET6_0 && !ANDROID && !IOS && !MACCATALYST && !WINDOWS)
	
#endif
}
