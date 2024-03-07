namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchStateChangedEventArgs"/>
/// </summary>
public class TouchStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="TouchStateChangedEventArgs"/>
	/// </summary>
	/// <param name="state"></param>
	public TouchStateChangedEventArgs(TouchState state)
		=> State = state;

	/// <summary>
	/// Gets the current state of the touch event.
	/// </summary>
	public TouchState State { get; }
}