namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
public class MauiOnDrawingEventArgs : EventArgs
{
	/// <summary>
	/// Initializes last drawing point
	/// </summary>
	/// <param name="point">Last drawing point</param>
	public MauiOnDrawingEventArgs(PointF point) => Point = point;

	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; }
}