namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
public class DrawingStartedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="DrawingStartedEventArgs"/>
	/// </summary>
	/// <param name="point"></param>
	public DrawingStartedEventArgs(PointF point)
	{
		Point = point;
	}

	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; }
}