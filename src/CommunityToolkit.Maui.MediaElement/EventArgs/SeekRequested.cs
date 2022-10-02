namespace CommunityToolkit.Maui.MediaElement;

class SeekRequested : EventArgs
{
	public TimeSpan Position { get; }

	public SeekRequested(TimeSpan position) => Position = position;
}
