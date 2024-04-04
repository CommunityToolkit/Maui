namespace CommunityToolkit.Maui.Core;

/// <summary>
/// <see cref="HoverStatusChangedEventArgs"/>
/// </summary>
public class HoverStatusChangedEventArgs(HoverStatus status) : EventArgs
{
	/// <summary>
	/// Gets the new <see cref="HoverStatus"/> of the element.
	/// </summary>
	public HoverStatus Status { get; } = status;
}