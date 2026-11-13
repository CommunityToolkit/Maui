namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing line
/// </summary>
public class DrawingLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initialize a new instance of <see cref="DrawingLineCompletedEventArgs"/>
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="lastDrawingLine"/> is null.</exception>
	public DrawingLineCompletedEventArgs(IDrawingLine lastDrawingLine)
	{
		ArgumentNullException.ThrowIfNull(lastDrawingLine);
		LastDrawingLine = lastDrawingLine;
	}

	/// <summary>
	/// Last drawing line
	/// </summary>
	public IDrawingLine LastDrawingLine { get; }
}