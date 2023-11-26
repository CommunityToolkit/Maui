namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
public class PointDrawnEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="PointDrawnEventArgs"/>
	/// </summary>
	/// <param name="point"></param>
	public PointDrawnEventArgs(PointF point)
	{
		Point = point;
	}

	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; }
}