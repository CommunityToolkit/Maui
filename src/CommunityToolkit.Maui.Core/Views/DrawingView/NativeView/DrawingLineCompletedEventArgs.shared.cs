namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Contains last drawing line
/// </summary>
public class DrawingNativeLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// Last drawing line
	/// </summary>
	public DrawingNativeLine? Line { get; }

	/// <summary>
	/// Initializes last drawing line
	/// </summary>
	/// <param name="line">Last drawing line</param>
	public DrawingNativeLineCompletedEventArgs(DrawingNativeLine? line)
	{
		Line = line;
	}
}