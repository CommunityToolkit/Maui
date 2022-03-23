using CommunityToolkit.Maui.Core.Views;

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
	public DrawingLineCompletedEventArgs(DrawingLine lastDrawingLine)
	{
		LastDrawingLine = lastDrawingLine;
	}

	/// <summary>
	/// Last drawing line
	/// </summary>
	public DrawingLine LastDrawingLine { get; }
}
