namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="HoverStateChangedEventArgs"/>
/// </summary>
public class HoverStateChangedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="HoverStateChangedEventArgs"/>
	/// </summary>
	/// <param name="state"></param>
	public HoverStateChangedEventArgs(HoverState state)
		=> State = state;

	/// <summary>
	/// Gets the new <see cref="HoverState"/> of the element.
	/// </summary>
	public HoverState State { get; }
}