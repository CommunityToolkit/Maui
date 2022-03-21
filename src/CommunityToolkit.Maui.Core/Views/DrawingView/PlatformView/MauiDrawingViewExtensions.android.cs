using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IDrawingView"/>
/// </summary>
public static partial class MauiDrawingViewExtensions
{
	/// <summary>
	/// Set MultiLine mode
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="multiLineMode">value</param>
	public static void SetMultiLineMode(this MauiDrawingView mauiDrawingView, bool multiLineMode)
	{
		mauiDrawingView.MultiLineMode = multiLineMode;
	}

	/// <summary>
	/// Set ClearOnFinish
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="clearOnFinish">value</param>
	public static void SetClearOnFinish(this MauiDrawingView mauiDrawingView, bool clearOnFinish)
	{
		mauiDrawingView.ClearOnFinish = clearOnFinish;
	}

	/// <summary>
	/// Set LineColor
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="lineColor">line color</param>
	public static void SetLineColor(this MauiDrawingView mauiDrawingView, Color lineColor)
	{
		mauiDrawingView.LineColor = lineColor.ToPlatform();
	}

	/// <summary>
	/// Set LineWidth
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="lineWidth">line width</param>
	public static void SetLineWidth(this MauiDrawingView mauiDrawingView, float lineWidth)
	{
		mauiDrawingView.LineWidth = lineWidth;
	}
}