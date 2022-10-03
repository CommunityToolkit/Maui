namespace CommunityToolkit.Maui.MediaElement;

class StateRequested : EventArgs
{
	public MediaElementState State { get; }

	public StateRequested(MediaElementState state) => State = state;
}
