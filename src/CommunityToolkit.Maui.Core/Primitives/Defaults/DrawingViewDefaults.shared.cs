using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for <see cref="IDrawingView"/>
/// </summary>
static class DrawingViewDefaults
{
	/// <summary>
	/// Minimum Granularity
	/// </summary>
	public const int MinimumGranularity = 5;

	/// <summary>
	/// Default Line Width
	/// </summary>
	public const float LineWidth = 5;

	/// <summary>
	/// Default Value for ShouldSmoothPathWhenDrawn
	/// </summary>
	public const bool ShouldSmoothPathWhenDrawn = true;

	/// <summary>
	/// Default Value for IsMultiLineModeEnabled
	/// </summary>
	public const bool IsMultiLineModeEnabled = false;

	/// <summary>
	/// Default Value for ShouldClearOnFinish
	/// </summary>
	public const bool ShouldClearOnFinish = false;

	/// <summary>
	/// Default Line Color
	/// </summary>
	public static Color LineColor { get; } = Colors.Black;

	/// <summary>
	/// Default Background Color
	/// </summary>
	public static Color BackgroundColor { get; } = Colors.LightGray;
}