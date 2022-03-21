using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IDrawingView"/>
/// </summary>
public static class MauiDrawingLineExtensions
{
	/// <summary>
	/// Set EnableSmoothedPath
	/// </summary>
	/// <param name="mauiDrawingLine"><see cref="MauiDrawingLine"/></param>
	/// <param name="enableSmoothedPath">value</param>
	public static void SetEnableSmoothedPath(this MauiDrawingLine mauiDrawingLine, bool enableSmoothedPath)
	{
		mauiDrawingLine.EnableSmoothedPath = enableSmoothedPath;
	}

	/// <summary>
	/// Set LineColor
	/// </summary>
	/// <param name="mauiDrawingLine"><see cref="MauiDrawingLine"/></param>
	/// <param name="lineColor">Line color</param>
	public static void SetLineColor(this MauiDrawingLine mauiDrawingLine, Color lineColor)
	{
		mauiDrawingLine.LineColor = lineColor;
	}

	/// <summary>
	/// Set LineWidth
	/// </summary>
	/// <param name="mauiDrawingLine"><see cref="MauiDrawingLine"/></param>
	/// <param name="lineWidth">Line width</param>
	public static void SetLineWidth(this MauiDrawingLine mauiDrawingLine, float lineWidth)
	{
		mauiDrawingLine.LineWidth = lineWidth;
	}

	/// <summary>
	/// Set Granularity
	/// </summary>
	/// <param name="mauiDrawingLine"><see cref="MauiDrawingLine"/></param>
	/// <param name="granularity">Granularity</param>
	public static void SetGranularity(this MauiDrawingLine mauiDrawingLine, int granularity)
	{
		mauiDrawingLine.Granularity = granularity;
	}
}