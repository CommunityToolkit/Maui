namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
/// <remarks>
/// Initialize a new instance of <see cref="PointDrawnEventArgs"/>
/// </remarks>
/// <param name="point"></param>
public class PointDrawnEventArgs(PointF point) : EventArgs
{
	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; } = point;
}