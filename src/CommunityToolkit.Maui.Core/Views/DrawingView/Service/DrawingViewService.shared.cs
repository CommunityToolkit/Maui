namespace CommunityToolkit.Maui.Core.Views;

public static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream, or <see cref="Stream.Null"/> when there is nothing to draw (no points, or an image below one pixel).</returns>
	public static ValueTask<Stream> GetImageStream(ImagePointOptions options, CancellationToken token = default) =>
		GetPlatformImageStream(options, token);

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream, or <see cref="Stream.Null"/> when there is nothing to draw (no lines, or no line with points).</returns>
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
/// <exception cref="ArgumentException">Thrown when the width or the height of <paramref name="desiredSize"/> is NaN, or when both are positive infinity; or when the width or the height of <paramref name="canvasSize"/> is NaN or positive infinity.</exception>
public abstract class ImageOptions(Size desiredSize, Paint? background, Size? canvasSize)
{
	/// <summary>
	/// Gets the desired dimensions of the generated image. The image will be resized proportionally.
	/// </summary>
	public Size DesiredSize { get; } = IsValidDesiredSize(desiredSize)
		? desiredSize
		: throw new ArgumentException($"The width and height of the desired size cannot be NaN, and only one of them can be positive infinity, but the desired size is {desiredSize}.", nameof(desiredSize));

	/// <summary>
	/// Gets the background <see cref="Paint"/> to apply to the output image.
	/// </summary>
	public Paint? Background { get; } = background;

	/// <summary>
	/// Gets the actual size of the canvas being displayed.
	/// if a value is provided then the contents of the drawing inside these dimensions will be included in the output,
	/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.
	/// </summary>
	public Size? CanvasSize { get; } = canvasSize is null || IsValidCanvasSize(canvasSize.Value)
		? canvasSize
		: throw new ArgumentException($"The width and height of the canvas size cannot be NaN or positive infinity, but the canvas size is {canvasSize}.", nameof(canvasSize));

	// The image is resized by the factor that fits it into the desired size, Math.Min(desired width / width, desired height / height)
	// (GetMaximumBitmap, GetMaximumUIImage, GetMaximumCanvasRenderTarget). A NaN side, or two sides of positive infinity, make that factor NaN
	// or infinite, and the platform image APIs then throw (Android) or abort the process (iOS, Mac Catalyst). One infinite side fits the image
	// to the other side, and a negative side, -Infinity included, gives the minimum image of one pixel.
	static bool IsValidDesiredSize(Size desiredSize) =>
		!double.IsNaN(desiredSize.Width)
		&& !double.IsNaN(desiredSize.Height)
		&& !(double.IsPositiveInfinity(desiredSize.Width) && double.IsPositiveInfinity(desiredSize.Height));

	// The canvas size is the size of the image that is drawn before the resize (GetBitmap, GetUIImage, GetCanvasRenderTarget), so a NaN side or a side
	// of positive infinity reaches the platform image APIs, which then throw (Android) or abort the process (iOS, Mac Catalyst). A canvas with a side
	// below one pixel, -Infinity included, gives Stream.Null.
	static bool IsValidCanvasSize(Size canvasSize) =>
		!double.IsNaN(canvasSize.Width)
		&& !double.IsNaN(canvasSize.Height)
		&& !double.IsPositiveInfinity(canvasSize.Width)
		&& !double.IsPositiveInfinity(canvasSize.Height);
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
/// <exception cref="ArgumentException">Thrown when the width or the height of <paramref name="desiredSize"/> is NaN, or when both are positive infinity; or when the width or the height of <paramref name="canvasSize"/> is NaN or positive infinity.</exception>
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
	/// Gets the lines that will be rendered in the resulting image.
	/// </summary>
	public IList<IDrawingLine> Lines { get; }

	/// <summary>
	/// Creates an instance of <see cref="ImageLineOptions"/> that will result in the output image covering the top-left to the bottom-right most points.
	/// </summary>
	/// <param name="lines">The lines that will be rendered in the resulting image.</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">The background <see cref="Paint"/> to apply to the output image.</param>
	/// <returns>An instance of <see cref="ImageLineOptions"/>.</returns>
	/// <exception cref="ArgumentException">Thrown when the width or the height of <paramref name="desiredSize"/> is NaN, or when both are positive infinity.</exception>
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
	/// <exception cref="ArgumentException">Thrown when the width or the height of <paramref name="desiredSize"/> is NaN, or when both are positive infinity; or when the width or the height of <paramref name="canvasSize"/> is NaN or positive infinity.</exception>
	public static ImageLineOptions FullCanvas(IList<IDrawingLine> lines, Size desiredSize, Paint? background, Size? canvasSize)
	{
		return new ImageLineOptions(lines, desiredSize, background, canvasSize);
	}
}