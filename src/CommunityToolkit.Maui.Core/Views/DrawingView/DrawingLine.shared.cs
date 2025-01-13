using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public class DrawingLine : IDrawingLine
{
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
		get;
		set => field = Math.Clamp(value, DrawingViewDefaults.MinimumGranularity, int.MaxValue);
	} = DrawingViewDefaults.MinimumGranularity;

	/// <summary>
	/// Enables or disables if this line is smoothed (anti-aliased) when drawn.
	/// </summary>
	public bool ShouldSmoothPathWhenDrawn { get; set; } = DrawingViewDefaults.ShouldSmoothPathWhenDrawn;

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="Point"/> that is provided as a parameter.
	/// </summary>
	/// <param name="points">A collection of <see cref="Point"/> that a image is generated from.</param>
	/// <param name="desiredSize">The desired dimensions of the generated image.</param>
	/// <param name="lineWidth">The desired line width to be used in the generated image.</param>
	/// <param name="strokeColor">The desired color of the line to be used in the generated image.</param>
	/// <param name="background">Background of the generated image.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="points"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(
		IEnumerable<PointF> points, 
		Size desiredSize, 
		float lineWidth,
		Color strokeColor,
		Paint background,
		CancellationToken token = default) =>
		GetImageStream(points, desiredSize, lineWidth, strokeColor, background, null, token);
	
	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="Point"/> that is provided as a parameter.
	/// </summary>
	/// <param name="points">A collection of <see cref="Point"/> that a image is generated from.</param>
	/// <param name="desiredSize">The desired dimensions of the generated image.</param>
	/// <param name="lineWidth">The desired line width to be used in the generated image.</param>
	/// <param name="strokeColor">The desired color of the line to be used in the generated image.</param>
	/// <param name="background">Background of the generated image.</param>
	/// <param name="canvasSize">
	/// The actual size of the canvas being displayed. This is an optional parameter
	/// if a value is provided then the contents of the <paramref name="points"/> inside these dimensions will be included in the output,
	/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.
	/// </param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="points"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(
		IEnumerable<PointF> points, 
		Size desiredSize, 
		float lineWidth,
		Color strokeColor,
		Paint background,
		Size? canvasSize = null,
		CancellationToken token = default) =>
		DrawingViewService.GetImageStream([.. points], desiredSize, lineWidth, strokeColor, background, canvasSize, token);

	/// <inheritdoc cref="IDrawingLine.GetImageStream(double, double, Paint, Size?, CancellationToken)"/>
	public ValueTask<Stream> GetImageStream(double desiredSizeWidth, double desiredSizeHeight, Paint background, Size? canvasSize = null, CancellationToken token = default) => 
		DrawingViewService.GetImageStream([.. Points], new Size(desiredSizeWidth, desiredSizeHeight), LineWidth, LineColor, background, canvasSize, token);
}