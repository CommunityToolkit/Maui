namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="TouchStatusChangedEventArgs"/>
/// </summary>
public class TouchStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="TouchStatusChangedEventArgs"/>
	/// </summary>
	/// <param name="status"></param>
	public TouchStatusChangedEventArgs(TouchStatus status)
		=> Status = status;

	/// <summary>
	/// Gets the current touch status.
	/// </summary>
	public TouchStatus Status { get; }
}