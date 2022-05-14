using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing line
/// </summary>
public class MauiDrawingLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Initializes last drawing line
	/// </summary>
	/// <param name="line">Last drawing line</param>
	public MauiDrawingLineCompletedEventArgs(MauiDrawingLine line) => Line = line;

	/// <summary>
	/// Last drawing line
	/// </summary>
	public MauiDrawingLine Line { get; }
}