using System.Collections.ObjectModel;
using System.Windows.Input;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The DrawingView allows you to draw one or multiple lines on a canvas
/// </summary>
public interface IDrawingView : IView
{
	/// <summary>
	/// Event occurred when drawing line completed
	/// </summary>
	/// <param name="drawingLineCompletedEventArgs">Last drawing <see cref="ILine"/></param>
	void DrawingLineCompleted(DrawingLineCompletedEventArgs drawingLineCompletedEventArgs);

	/// <summary>
	/// The <see cref="Color"/> that is used by default to draw a line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	Color LineColor { get; }

	/// <summary>
	/// The width that is used by default to draw a line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	float LineWidth { get; }

	/// <summary>
	/// This command is invoked whenever the drawing of a line on <see cref="IDrawingView"/> has completed.
	/// Note that this is fired after the tap or click is lifted. When <see cref="MultiLineMode"/> is enabled
	/// this command is fired multiple times.
	/// This is a bindable property.
	/// </summary>
	ICommand? DrawingLineCompletedCommand { get; }

	/// <summary>
	/// The collection of lines that are currently on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	ObservableCollection<ILine> Lines { get; }

	/// <summary>
	/// Toggles multi-line mode. When true, multiple lines can be drawn on the <see cref="IDrawingView"/> while the tap/click is released in-between lines.
	/// Note: when <see cref="ClearOnFinish"/> is also enabled, the lines are cleared after the tap/click is released.
	/// Additionally, <see cref="DrawingLineCompletedCommand"/> will be fired after each line that is drawn.
	/// This is a bindable property.
	/// </summary>
	bool MultiLineMode { get; }

	/// <summary>
	/// Indicates whether the <see cref="IDrawingView"/> is cleared after releasing the tap/click and a line is drawn.
	/// Note: when <see cref="MultiLineMode"/> is also enabled, this might cause unexpected behavior.
	/// This is a bindable property.
	/// </summary>
	bool ClearOnFinish { get; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the <see cref="Lines"/> that are currently drawn on the <see cref="IDrawingView"/>.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	Stream GetImageStream(double imageSizeWidth, double imageSizeHeight);
}