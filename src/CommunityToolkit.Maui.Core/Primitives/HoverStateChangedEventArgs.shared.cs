namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="HoverStateChangedEventArgs"/>
/// </summary>
public class HoverStateChangedEventArgs(HoverState state) : EventArgs
{
	/// <summary>
	/// Gets the new <see cref="HoverState"/> of the element.
	/// </summary>
	public HoverState State { get; } = state;
}