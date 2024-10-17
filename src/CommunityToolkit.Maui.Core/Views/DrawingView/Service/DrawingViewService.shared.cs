namespace CommunityToolkit.Maui.Core.Views;

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
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<PointF> points, Size desiredSize, float lineWidth, Color strokeColor, Paint? background, CancellationToken token = default) =>
		GetImageStream(points, desiredSize, lineWidth, strokeColor, background, null, token);	

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="desiredSize">The desired dimensions of the generated image. The image will be resized proportionally.</param>
	/// <param name="background">Image background</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<IDrawingLine> lines, Size desiredSize, Paint? background, CancellationToken token = default) =>
		GetImageStream(lines, desiredSize, background, null, token);
}