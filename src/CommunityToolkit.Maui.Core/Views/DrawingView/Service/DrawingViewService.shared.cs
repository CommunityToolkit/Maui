namespace CommunityToolkit.Maui.Core.Views;

public static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(ImagePointOptions options, CancellationToken token = default) =>
		GetPlatformImageStream(options, token);

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(ImageLineOptions options, CancellationToken token = default) =>
		GetPlatformImageStream(options, token);
}

/// <summary>
/// Base class set of options used for generating an image from an <see cref="IDrawingView"/>.
/// </summary>
/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
/// <param name="background">The background <see cref="Paint"/> to apply to the output image.</param>
/// <param name="canvasSize">The actual size of the canvas being displayed.
/// if a value is provided then the contents of the drawing inside these dimensions will be included in the output,
/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.</param>
public abstract class ImageOptions(Size desiredSize, Paint? background, Size? canvasSize)
{
	/// <summary>
	/// Gets the desired dimensions of the generated image. The image will be resized proportionally.
	/// </summary>
	public Size DesiredSize { get; } = desiredSize;

	/// <summary>
	/// Gets the background <see cref="Paint"/> to apply to the output image.
	/// </summary>
	public Paint? Background { get; } = background;

	/// <summary>
	/// Gets the actual size of the canvas being displayed.
	/// if a value is provided then the contents of the drawing inside these dimensions will be included in the output,
	/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.
	/// </summary>
	public Size? CanvasSize { get; } = canvasSize;
}

/// <summary>
/// Represents a set of options that controls how a set of points will be output to an image.
/// </summary>
/// <param name="points">The points that will result in a single line.</param>
/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
/// <param name="lineWidth">The width of the line to render.</param>
/// <param name="strokeColor">The <see cref="Color"/> of the line to render.</param>
/// <param name="background">The background <see cref="Paint"/> to apply to the output image.</param>
/// <param name="canvasSize">The actual size of the canvas being displayed.
/// if a value is provided then the contents of the drawing inside these dimensions will be included in the output,
/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.</param>
public class ImagePointOptions(IList<PointF> points, Size desiredSize, float lineWidth, Color strokeColor, Paint? background, Size? canvasSize) : ImageOptions(desiredSize, background, canvasSize)
{
	/// <summary>
	/// Gets the points that will result in a single line.
	/// </summary>
	public IList<PointF> Points { get; } = points;

	/// <summary>
	/// Gets the width of the line to render.
	/// </summary>
	public float LineWidth { get; } = lineWidth;

	/// <summary>
	/// Gets the <see cref="Color"/> of the line to render.
	/// </summary>
	public Color StrokeColor { get; } = strokeColor;
}

/// <summary>
/// Represents a set of options that controls how a set of lines will be output to an image.
/// </summary>
public class ImageLineOptions : ImageOptions
{
	ImageLineOptions(IList<IDrawingLine> lines, Size desiredSize, Paint? background, Size? canvasSize)
		: base(desiredSize, background, canvasSize)
	{
		Lines = lines;
	}

	/// <summary>
	/// Creates an instance of <see cref="ImageLineOptions"/> that will result in the output image covering the top-left to the bottom-right most points.
	/// </summary>
	/// <param name="lines">The lines that will be rendered in the resulting image.</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">The background <see cref="Paint"/> to apply to the output image.</param>
	/// <returns>An instance of <see cref="ImageLineOptions"/>.</returns>
	public static ImageLineOptions JustLines(IList<IDrawingLine> lines, Size desiredSize, Paint? background)
	{
		return new ImageLineOptions(lines, desiredSize, background, null);
	}

	/// <summary>
	/// Creates an instance of <see cref="ImageLineOptions"/> that will result in the contents of the drawing inside the supplied <paramref name="canvasSize"/> will be included in the output,
	/// </summary>
	/// <param name="lines">The lines that will be rendered in the resulting image.</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">The background <see cref="Paint"/> to apply to the output image.</param>
	/// <param name="canvasSize">The actual size of the canvas being displayed.</param>
	/// <returns>An instance of <see cref="ImageLineOptions"/>.</returns>
	public static ImageLineOptions FullCanvas(IList<IDrawingLine> lines, Size desiredSize, Paint? background, Size? canvasSize)
	{
		return new ImageLineOptions(lines, desiredSize, background, canvasSize);
	}

	/// <summary>
	/// Gets the lines that will be rendered in the resulting image.
	/// </summary>
	public IList<IDrawingLine> Lines { get; }
}