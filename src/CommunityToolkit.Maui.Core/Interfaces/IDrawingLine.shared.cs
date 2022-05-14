using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public interface IDrawingLine
{
	/// <summary>
	/// The granularity of this line. Min value is 5. The higher the value, the smoother the line, the slower the program.
	/// </summary>
	int Granularity { get; set; }

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	Color LineColor { get; set; }

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	float LineWidth { get; set; }

	/// <summary>
	/// The collection of <see cref="PointF"/> that makes up this line on the <see cref="IDrawingView"/>.
	/// </summary>
	ObservableCollection<PointF> Points { get; set; }

	/// <summary>
	/// Enables or disables if this line is smoothed (anti-aliased) when drawn.
	/// </summary>
	bool ShouldSmoothPathWhenDrawn { get; set; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned.</param>
	/// <param name="background">Background of the generated image.</param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingLine"/>.</returns>
	ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, Paint background);
}