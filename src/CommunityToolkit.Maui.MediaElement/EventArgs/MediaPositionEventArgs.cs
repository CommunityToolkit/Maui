using System;
namespace CommunityToolkit.Maui.MediaElement;

public class MediaPositionEventArgs : EventArgs
{
	public TimeSpan Position { get; private set; }

	public MediaPositionEventArgs(TimeSpan position)
	{
		Position = position;
	}
}