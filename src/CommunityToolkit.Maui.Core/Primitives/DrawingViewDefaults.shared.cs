using System;
using System.ComponentModel;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Default Values for <see cref="IDrawingView"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DrawingViewDefaults
{
	/// <summary>
	/// Default Line Width
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public const float LineWidth = 5;

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

