namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing point
/// </summary>
public class OnDrawingEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="OnDrawingEventArgs"/>
	/// </summary>
	/// <param name="point"></param>
	public OnDrawingEventArgs(PointF point)
	{
		Point = point;
	}

	/// <summary>
	/// Last drawing point
	/// </summary>
	public PointF Point { get; }
}