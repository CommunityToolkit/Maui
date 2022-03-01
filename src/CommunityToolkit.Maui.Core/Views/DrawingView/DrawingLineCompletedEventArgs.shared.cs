namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Contains last drawing line
/// </summary>
public class DrawingLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Last drawing line
	/// </summary>
	public ILine? Line { get; }

	/// <summary>
	/// Initializes last drawing line
	/// </summary>
	/// <param name="line">Last drawing line</param>
	public DrawingLineCompletedEventArgs(ILine? line)
	{
		Line = line;
	}
}