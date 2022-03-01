using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="DrawingView"/>.
/// </summary>
public class Line : BindableObject, ILine
{
	const int minValueGranularity = 5;

	/// <summary>
	/// Backing BindableProperty for the <see cref="Granularity"/> property.
	/// </summary>
	public static readonly BindableProperty GranularityProperty =
		BindableProperty.Create(nameof(Granularity), typeof(int), typeof(Line), minValueGranularity,
			coerceValue: CoerceValue);

	/// <summary>
	/// Backing BindableProperty for the <see cref="EnableSmoothedPath"/> property.
	/// </summary>
	public static readonly BindableProperty EnableSmoothedPathProperty =
		BindableProperty.Create(nameof(EnableSmoothedPath), typeof(bool), typeof(Line), true);

	/// <summary>
	/// Backing BindableProperty for the <see cref="Points"/> property.
	/// </summary>
	public static readonly BindableProperty PointsProperty = BindableProperty.Create(
		nameof(Points), typeof(ObservableCollection<Point>), typeof(Line), new ObservableCollection<Point>(),
		BindingMode.TwoWay);

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineColor"/> property.
	/// </summary>
	public static readonly BindableProperty LineColorProperty =
		BindableProperty.Create(nameof(LineColor), typeof(Color), typeof(Line), Colors.Black);

	/// <summary>
	/// Backing BindableProperty for the <see cref="LineWidth"/> property.
	/// </summary>
	public static readonly BindableProperty LineWidthProperty =
		BindableProperty.Create(nameof(LineWidth), typeof(float), typeof(Line), 5f);

	/// <summary>
	/// Initializes a new Line object.
	/// </summary>
	public Line() => Points = new ObservableCollection<Point>();

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public Color LineColor
	{
		get => (Color)GetValue(LineColorProperty);
		set => SetValue(LineColorProperty, value);
	}

	/// <summary>
	/// The width that is used to draw this line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth
	{
		get => (float)GetValue(LineWidthProperty);
		set => SetValue(LineWidthProperty, value);
	}

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="DrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<Point> Points
	{
		get => (ObservableCollection<Point>)GetValue(PointsProperty);
		set => SetValue(PointsProperty, value);
	}

	/// <summary>
	/// The granularity of this line. This is a bindable property.
	/// </summary>
	public int Granularity
	{
		get => (int)GetValue(GranularityProperty);
		set => SetValue(GranularityProperty, value);
	}

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn. This is a bindable property.
	/// </summary>
	public bool EnableSmoothedPath
	{
		get => (bool)GetValue(EnableSmoothedPathProperty);
		set => SetValue(EnableSmoothedPathProperty, value);
	}

	static object CoerceValue(BindableObject bindable, object value)
		=> ((Line)bindable).CoerceValue((int)value);

	int CoerceValue(int value) => value < minValueGranularity ? minValueGranularity : value;

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired heigth of the image that is returned.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's currently on the <see cref="DrawingView"/>.</returns>
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