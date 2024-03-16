namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchStateChangedEventArgs"/>
/// </summary>
public class TouchStateChangedEventArgs(TouchState state) : EventArgs
{
	/// <summary>
	/// Gets the current state of the touch event.
	/// </summary>
	public TouchState State { get; } = state;
}