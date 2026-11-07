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
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="line"/> is null.</exception>
	public MauiDrawingLineCompletedEventArgs(MauiDrawingLine line)
	{
		ArgumentNullException.ThrowIfNull(line);
		Line = line;
	}

	/// <summary>
	/// Last drawing line
	/// </summary>
	public MauiDrawingLine Line { get; }
}