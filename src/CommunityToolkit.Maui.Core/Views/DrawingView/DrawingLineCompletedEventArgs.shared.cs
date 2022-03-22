using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Contains last drawing line
/// </summary>
public class DrawingLineCompletedEventArgs : EventArgs
{
	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public Color LineColor { get; init; } = Colors.Black;

	/// <summary>
	/// The width that is used to draw this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth { get; init; }

	/// <summary>
	/// The collection of <see cref="Point"/> that makes up this line on the <see cref="IDrawingView"/>. This is a bindable property.
	/// </summary>
	public ObservableCollection<Point> Points { get; init; } = new();

	/// <summary>
	/// The granularity of this line. This is a bindable property.
	/// </summary>
	public int Granularity { get; init; }

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn. This is a bindable property.
	/// </summary>
	public bool EnableSmoothedPath { get; init; }
}