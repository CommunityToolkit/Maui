using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public class DrawingLine : IDrawingLine
{
	const int minValueGranularity = 5;
	int granularity = minValueGranularity;

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public Color LineColor { get; set; } = Colors.Black;

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public float LineWidth { get; set; } = 5;

	/// <summary>
	/// The collection of <see cref="PointF"/> that makes up this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public ObservableCollection<PointF> Points { get; set; } = new();

	/// <summary>
	/// The granularity of this line. Min value is 5. The higher the value, the smoother the line, the slower the program. Value clamped between <see cref="minValueGranularity"/> and <see cref="int.MaxValue"/>
	/// </summary>
	public int Granularity
	{
		get => granularity;
		set => granularity = Math.Clamp(value, minValueGranularity, int.MaxValue);
	}

	/// <summary>
	/// Enables or disables if this line is smoothed (anti-aliased) when drawn.
	/// </summary>
	public bool EnableSmoothedPath { get; set; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	public ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, Color backgroundColor) =>
		DrawingViewService.GetImageStream(Points.ToList(), new Size(imageSizeWidth, imageSizeHeight), LineWidth, LineColor, backgroundColor);

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="Point"/> that is provided as a parameter.
	/// </summary>
	/// <param name="points">A collection of <see cref="Point"/> that a image is generated from.</param>
	/// <param name="imageSize">The desired dimensions of the generated image.</param>
	/// <param name="lineWidth">The desired line width to be used in the generated image.</param>
	/// <param name="strokeColor">The desired color of the line to be used in the generated image.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="points"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(IEnumerable<PointF> points,
										Size imageSize,
										float lineWidth,
										Color strokeColor,
										Color backgroundColor)
		=> DrawingViewService.GetImageStream(points.ToList(), imageSize, lineWidth, strokeColor, backgroundColor);
}