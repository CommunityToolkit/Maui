using System;
namespace CommunityToolkit.Maui.MediaElement;

public sealed class MediaPositionEventArgs : EventArgs
{
	public TimeSpan Position { get; }

	public MediaPositionEventArgs(TimeSpan position)
	{
		Position = position;
	}
}