using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public class DrawingNativeLine
{
	int granularity;
	const int minValueGranularity = 5;

	/// <summary>
	/// Initializes a new Line object.
	/// </summary>
	public DrawingNativeLine()
	{
		Points = new ObservableCollection<Point>();
		LineColor = Colors.Black;
		LineWidth = 5;
		Granularity = minValueGranularity;
	}

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public Color LineColor { get; set; }

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth { get; set; }

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<Point> Points { get; set; }

	/// <summary>
	/// The granularity of this line. This is a bindable property.
	/// </summary>
	public int Granularity
	{
		get => granularity;
		set => granularity = value < minValueGranularity ? minValueGranularity : value;
	}

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn. This is a bindable property.
	/// </summary>
	public bool EnableSmoothedPath { get; set; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired heigth of the image that is returned.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	public Stream GetImageStream(double imageSizeWidth, double imageSizeHeight, Color backgroundColor) =>
		DrawingViewService.GetImageStream(Points.ToList(), new Size(imageSizeWidth, imageSizeHeight), LineWidth,
			LineColor,
			backgroundColor);

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of the collection of <see cref="Point"/> that is provided as a parameter.
	/// </summary>
	/// <param name="points">A collection of <see cref="Point"/> that a image is generated from.</param>
	/// <param name="imageSize">The desired dimensions of the generated image.</param>
	/// <param name="lineWidth">The desired line width to be used in the generated image.</param>
	/// <param name="strokeColor">The desired color of the line to be used in the generated image.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's provided through the <paramref name="points"/> parameter.</returns>
	public static Stream GetImageStream(IEnumerable<Point> points,
		Size imageSize,
		float lineWidth,
		Color strokeColor,
		Color backgroundColor) =>
		DrawingViewService.GetImageStream(points.ToList(), imageSize, lineWidth, strokeColor, backgroundColor);
}