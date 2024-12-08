namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
/// <remarks>
/// Initialize a new instance of <see cref="DrawingLineStartedEventArgs"/>
/// </remarks>
/// <param name="point"></param>
public class DrawingLineStartedEventArgs(PointF point) : EventArgs
{
	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; } = point;
}