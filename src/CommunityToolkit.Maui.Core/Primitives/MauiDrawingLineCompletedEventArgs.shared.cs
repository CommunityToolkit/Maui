using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Contains last drawing line
/// </summary>
/// <remarks>
/// Initializes last drawing line
/// </remarks>
/// <param name="line">Last drawing line</param>
public class MauiDrawingLineCompletedEventArgs(MauiDrawingLine line) : EventArgs
{
	/// <summary>
	/// Last drawing line
	/// </summary>
	public MauiDrawingLine Line { get; } = line;
}