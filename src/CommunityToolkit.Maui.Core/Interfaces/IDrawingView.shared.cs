using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas
/// </summary>
public interface IDrawingView : IView
{
	/// <summary>
	/// The <see cref="Color"/> that is used by default to draw a line on the <see cref="IDrawingView"/>.
	/// </summary>
	Color LineColor { get; }

	/// <summary>
	/// The width that is used by default to draw a line on the <see cref="IDrawingView"/>.
	/// </summary>
	float LineWidth { get; }

	/// <summary>
	/// The collection of lines that are currently on the <see cref="IDrawingView"/>.
	/// </summary>
	ObservableCollection<IDrawingLine> Lines { get; }

	/// <summary>
	/// Toggles multi-line mode. When true, multiple lines can be drawn on the <see cref="IDrawingView"/> while the tap/click is released in-between lines.
	/// Note: when <see cref="ShouldClearOnFinish"/> is also enabled, the lines are cleared after the tap/click is released.
	/// Additionally, <see cref="DrawingLineCompleted"/> will be fired after each line that is drawn.
	/// </summary>
	bool IsMultiLineModeEnabled { get; }

	/// <summary>
	/// Indicates whether the <see cref="IDrawingView"/> is cleared after releasing the tap/click and a line is drawn.
	/// Note: when <see cref="IsMultiLineModeEnabled"/> is also enabled, this might cause unexpected behavior.
	/// </summary>
	bool ShouldClearOnFinish { get; }

	/// <summary>
	/// Allows to draw on the <see cref="IDrawingView"/>.
	/// </summary>
	Action<ICanvas, RectF>? DrawAction { get; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the <see cref="Lines"/> that are currently drawn on the <see cref="IDrawingView"/>.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned. The image will be resized proportionally.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned. The image will be resized proportionally.</param>
	/// <returns><see cref="Task{Stream}"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight);

	/// <summary>
	/// Clears the <see cref="Lines"/> that are currently drawn on the <see cref="IDrawingView"/>.
	/// </summary>
	void Clear();

	/// <summary>
	/// Event occurred when drawing line completed
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	void DrawingLineCompleted(IDrawingLine lastDrawingLine);
}