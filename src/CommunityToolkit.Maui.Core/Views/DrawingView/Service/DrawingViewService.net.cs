namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Drawing view service
/// </summary>
public static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="background">Image background</param>
	/// <param name="canvasSize">
	/// The actual size of the canvas being displayed. This is an optional parameter
	/// if a value is provided then the contents of the <paramref name="points"/> inside these dimensions will be included in the output,
	/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.
	/// </param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<PointF> points, Size desiredSize, float lineWidth, Color strokeColor, Paint? background, Size? canvasSize, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Stream.Null);
	}

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">Image background</param>
	/// <param name="canvasSize">
	/// The actual size of the canvas being displayed. This is an optional parameter
	/// if a value is provided then the contents of the <paramref name="lines"/> inside these dimensions will be included in the output,
	/// if <c>null</c> is provided then the resulting output will be the area covered by the top-left to the bottom-right most points.
	/// </param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<IDrawingLine> lines, Size desiredSize, Paint? background, Size? canvasSize, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();
		return ValueTask.FromResult(Stream.Null);
	}
}