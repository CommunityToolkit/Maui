namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Enumeration of the options available when generating an image stream using the DrawingView.
/// </summary>
public enum DrawingViewOutputOption
{
	/// <summary>
	/// Outputs the area covered by the top-left to the bottom-right most points.
	/// </summary>
	Lines,

	/// <summary>
	/// Outputs the full area displayed within the drawing view.
	/// </summary>
	FullCanvas
}