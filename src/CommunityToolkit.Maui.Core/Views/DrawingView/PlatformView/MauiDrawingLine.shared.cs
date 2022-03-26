namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Line object is used to describe the lines that are drawn on a <see cref="MauiDrawingView"/>.
/// </summary>
public partial class MauiDrawingLine
{
	const int minValueGranularity = 5;

	int granularity = minValueGranularity;

	/// <summary>
	/// The width that is used to draw this line on the <see cref="MauiDrawingView"/>. This is a bindable property.
	/// </summary>
	public float LineWidth { get; set; } = 5;

	/// <summary>
	/// The granularity of this line. This is a bindable property. Clamped to a minimum value of 5.
	/// </summary>
	public int Granularity
	{
		get => granularity;
		set => granularity = Math.Clamp(value, minValueGranularity, int.MaxValue);
	}

	/// <summary>
	/// Enables or disabled if this line is smoothed (anti-aliased) when drawn. This is a bindable property.
	/// </summary>
	public bool EnableSmoothedPath { get; set; } = true;
}