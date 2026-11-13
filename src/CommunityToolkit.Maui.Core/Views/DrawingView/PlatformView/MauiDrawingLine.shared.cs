using System.Collections.ObjectModel;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public class MauiDrawingLine
{
	/// <summary>
	/// The width that is used to draw this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public float LineWidth { get; set; } = DrawingViewDefaults.LineWidth;

	/// <summary>
	/// The <see cref="Color"/> that is used to draw this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public Color LineColor { get; set; } = DrawingViewDefaults.LineColor;

	/// <summary>
	/// The collection of <see cref="PointF"/> that makes up this line on the <see cref="MauiDrawingView"/>.
	/// </summary>
	public ObservableCollection<PointF> Points { get; set; } = [];

	/// <summary>
	/// The granularity of this line. Clamped to a minimum value of 5. Value clamped between <see cref="DrawingViewDefaults.MinimumGranularity"/> and <see cref="int.MaxValue"/>
	/// </summary>
	public int Granularity
	{
		get;
		set => field = Math.Clamp(value, DrawingViewDefaults.MinimumGranularity, int.MaxValue);
	} = DrawingViewDefaults.MinimumGranularity;

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn.
	/// </summary>
	public bool ShouldSmoothPathWhenDrawn { get; set; } = true;
}