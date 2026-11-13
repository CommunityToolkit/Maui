namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
/// <remarks>
/// Initializes last drawing point
/// </remarks>
/// <param name="point">Last drawing point</param>
public class MauiDrawingStartedEventArgs(PointF point) : EventArgs
{
	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; } = point;
}