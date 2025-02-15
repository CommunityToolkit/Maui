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
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/> </param>
	/// <returns><see cref="ValueTask{Stream}"/> containing the data of the requested image with data that's provided through the <paramref name="options"/> parameter.</returns>
	public static ValueTask<Stream> GetImageStream(
		ImagePointOptions options,
		CancellationToken token = default) =>
		DrawingViewService.GetImageStream(options, token);

	/// <inheritdoc cref="IDrawingLine.GetImageStream(double, double, Paint, Size?, CancellationToken)"/>
	public ValueTask<Stream> GetImageStream(double desiredSizeWidth, double desiredSizeHeight, Paint background, Size? canvasSize = null, CancellationToken token = default) =>
		DrawingViewService.GetImageStream(new ImagePointOptions([.. Points], new Size(desiredSizeWidth, desiredSizeHeight), LineWidth, LineColor, background, canvasSize), token);
}