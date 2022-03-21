using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="IDrawingView"/>.
/// </summary>
public interface ILine : IElement
{
	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	Color LineColor { get; }

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	float LineWidth { get; }

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	ObservableCollection<Point> Points { get; }

	/// <summary>
	/// The granularity of this line. This is a bindable property.
	/// </summary>
	int Granularity { get; }

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn. This is a bindable property.
	/// </summary>
	bool EnableSmoothedPath { get; }

	/// <summary>
	/// Retrieves a <see cref="Stream"/> containing an image of this line, based on the <see cref="Points"/> data.
	/// </summary>
	/// <param name="imageSizeWidth">Desired width of the image that is returned.</param>
	/// <param name="imageSizeHeight">Desired heigth of the image that is returned.</param>
	/// <param name="backgroundColor">Background color of the generated image.</param>
	/// <returns><see cref="Stream"/> containing the data of the requested image with data that's currently on the <see cref="IDrawingView"/>.</returns>
	Stream GetImageStream(double imageSizeWidth, double imageSizeHeight, Color backgroundColor);
}