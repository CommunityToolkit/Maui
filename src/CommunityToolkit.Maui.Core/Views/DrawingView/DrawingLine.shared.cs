using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public class DrawingLine : IDrawingLine
{
	int granularity = DrawingViewDefaults.MinimumGranularity;

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public Color LineColor { get; set; } = DrawingViewDefaults.LineColor;

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public float LineWidth { get; set; } = DrawingViewDefaults.LineWidth;

	/// <summary>
	/// The collection of <see cref="PointF"/> that makes up this line on the <see cref="IDrawingView"/>.
	/// </summary>
	public ObservableCollection<PointF> Points { get; set; } = [];

	/// <summary>
	/// The granularity of this line. Min value is 5. The higher the value, the smoother the line, the slower the program. Value clamped between <see cref="DrawingViewDefaults.MinimumGranularity"/> and <see cref="int.MaxValue"/>
	/// </summary>
	public int Granularity
	{
		get => granularity;
		set => granularity = Math.Clamp(value, DrawingViewDefaults.MinimumGranularity, int.MaxValue);
	}

	/// <summary>
	/// Enables or disables if this line is smoothed (anti-aliased) when drawn.
	/// </summary>
	public bool ShouldSmoothPathWhenDrawn { get; set; } = DrawingViewDefaults.ShouldSmoothPathWhenDrawn;

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="Point"/> that is provided as a parameter.
	/// </summary>
	/// <param name="points">A collection of <see cref="Point"/> that a image is generated from.</param>
	/// <param name="imageSize">The desired dimensions of the generated image.</param>
	/// <param name="lineWidth">The desired line width to be used in the generated image.</param>
	/// <param name="strokeColor">The desired color of the line to be used in the generated image.</param>
	/// <param name="background">Background of the generated image.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="points"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(IEnumerable<PointF> points,
										Size imageSize,
										float lineWidth,
										Color strokeColor,
										Paint background,
										CancellationToken token = default)
	{
		return DrawingViewService.GetImageStream(points.ToList(), imageSize, lineWidth, strokeColor, background, token);
	}

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired height of the image that is returned.</param>
	/// <param name="background">Background of the generated image.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	public ValueTask<Stream> GetImageStream(double imageSizeWidth, double imageSizeHeight, Paint background, CancellationToken token = default)
	{
		return DrawingViewService.GetImageStream(Points.ToList(), new Size(imageSizeWidth, imageSizeHeight), LineWidth, LineColor, background, token);
	}
}