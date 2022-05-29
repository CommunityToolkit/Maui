using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for <see cref="IDrawingView"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DrawingViewDefaults
{
	/// <summary>
	/// Minimum Granularity
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const int MinimumGranularity = 5;

	/// <summary>
	/// Default Line Width
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const float LineWidth = 5;

	/// <summary>
	/// Default Value for ShouldSmoothPathWhenDrawn
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool ShouldSmoothPathWhenDrawn = true;

	/// <summary>
	/// Default Value for IsMultiLineModeEnabled
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool IsMultiLineModeEnabled = false;

	/// <summary>
	/// Default Value for ShouldClearOnFinish
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const bool ShouldClearOnFinish = false;

	/// <summary>
	/// Default Line Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color LineColor { get; } = Colors.Black;

	/// <summary>
	/// Default Background Color
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Color BackgroundColor { get; } = Colors.LightGray;
}