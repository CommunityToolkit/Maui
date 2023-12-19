namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
public class DrawingLineStartedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="DrawingLineStartedEventArgs"/>
	/// </summary>
	/// <param name="point"></param>
	public DrawingLineStartedEventArgs(PointF point)
	{
		Point = point;
	}

	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; }
}