namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="HoverStatusChangedEventArgs"/>
/// </summary>
public class HoverStatusChangedEventArgs : EventArgs
{
	/// <summary>
	/// Constructor for <see cref="HoverStatusChangedEventArgs"/>
	/// </summary>
	/// <param name="status"></param>
	public HoverStatusChangedEventArgs(HoverStatus status)
		=> Status = status;

	/// <summary>
	/// Gets the new <see cref="HoverStatus"/> of the element.
	/// </summary>
	public HoverStatus Status { get; }
}