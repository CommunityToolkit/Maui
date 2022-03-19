using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IDrawingView"/>
/// </summary>
static partial class MauiDrawingViewExtensions
{
	public static void SetMultiLineMode(this MauiDrawingView mauiDrawingView, bool multiLineMode)
	{
		mauiDrawingView.MultiLineMode = multiLineMode;
	}

	public static void SetClearOnFinish(this MauiDrawingView mauiDrawingView, bool clearOnFinish)
	{
		mauiDrawingView.ClearOnFinish = clearOnFinish;
	}

	public static void SetLineColor(this MauiDrawingView mauiDrawingView, Color lineColor)
	{
		mauiDrawingView.LineColor = lineColor.ToPlatform();
	}

	public static void SetLineWidth(this MauiDrawingView mauiDrawingView, float lineColor)
	{
		mauiDrawingView.LineWidth = lineColor;
	}

	public static void SetDrawingLineCompletedCommand(this MauiDrawingView mauiDrawingView, ICommand? drawingLineCompletedCommand)
	{
		mauiDrawingView.DrawingLineCompletedCommand = drawingLineCompletedCommand;
	}
}