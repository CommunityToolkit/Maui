namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchStatusChangedEventArgs"/>
/// </summary>
public class TouchStatusChangedEventArgs(TouchStatus status) : EventArgs
{
	/// <summary>
	/// Gets the current touch status.
	/// </summary>
	public TouchStatus Status { get; } = status;
}