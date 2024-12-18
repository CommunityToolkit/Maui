namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing line
/// </summary>
/// <remarks>
/// Initialize a new instance of <see cref="DrawingLineCompletedEventArgs"/>
/// </remarks>
/// <param name="lastDrawingLine">Last drawing line</param>
public class DrawingLineCompletedEventArgs(IDrawingLine lastDrawingLine) : EventArgs
{
	/// <summary>
	/// Last drawing line
	/// </summary>
	public IDrawingLine LastDrawingLine { get; } = lastDrawingLine;
}