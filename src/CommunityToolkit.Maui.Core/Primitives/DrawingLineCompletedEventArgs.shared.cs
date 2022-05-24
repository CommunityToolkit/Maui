namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing line
/// </summary>
public class DrawingLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="DrawingLineCompletedEventArgs"/>
	/// </summary>
	/// <param name="lastDrawingLine"></param>
	public DrawingLineCompletedEventArgs(IDrawingLine lastDrawingLine)
	{
		LastDrawingLine = lastDrawingLine;
	}

	/// <summary>
	/// Last drawing line
	/// </summary>
	public IDrawingLine LastDrawingLine { get; }
}