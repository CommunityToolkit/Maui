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

	/// <summary>
	/// Set Lines
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/><see cref="MauiDrawingView"/></param>
	/// <param name="drawingView"><see cref="IDrawingView"/></param>
	public static void SetLines(this MauiDrawingView mauiDrawingView, IDrawingView drawingView)
	{
		if (mauiDrawingView.Lines.Count == drawingView.Lines.Count)
		{
			return;
		}

		IReadOnlyList<DrawingLine> lines = drawingView.Lines.ToList();
		if (!drawingView.MultiLineMode && drawingView.Lines.Count > 1)
		{
			lines = lines.TakeLast(1).ToList();
		}

		try
		{
			mauiDrawingView.Lines.Clear();

			foreach (var line in lines)
			{
				mauiDrawingView.Lines.Add(new MauiDrawingLine()
				{
					LineColor = line.LineColor.ToPlatform(),
					EnableSmoothedPath = line.EnableSmoothedPath,
					Granularity = line.Granularity,
					LineWidth = line.LineWidth,
					Points = line.Points.Select(p => new Point((float)p.X, (float)p.Y)).ToObservableCollection()
				});
			}
		}
		catch (InvalidOperationException) 
		{
			// Ignore System.InvalidOperationException: Cannot change ObservableCollection during a CollectionChanged event.
		}
	}

	/// <summary>
	/// Set Lines
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/><see cref="MauiDrawingView"/></param>
	/// <param name="drawingView"><see cref="IDrawingView"/></param>
	public static void SetLines(this IDrawingView drawingView, MauiDrawingView mauiDrawingView)
	{
		if (mauiDrawingView.Lines.Count == drawingView.Lines.Count)
		{
			return;
		}

		IReadOnlyList<MauiDrawingLine> lines = mauiDrawingView.Lines.ToList();
		if (!mauiDrawingView.MultiLineMode && mauiDrawingView.Lines.Count > 1)
		{
			lines = lines.TakeLast(1).ToList();
		}

		try
		{ 
			drawingView.Lines.Clear();

			foreach (var line in lines)
			{
				drawingView.Lines.Add(new DrawingLine()
				{
					LineColor = line.LineColor.ToColor() ?? Colors.Transparent,
					EnableSmoothedPath = line.EnableSmoothedPath,
					Granularity = line.Granularity,
					LineWidth = line.LineWidth,
					Points = line.Points.Select(p => new Point(p.X, p.Y)).ToObservableCollection()
				});
			}
		}
		catch (InvalidOperationException) 
		{
			// Ignore System.InvalidOperationException: Cannot change ObservableCollection during a CollectionChanged event.
		}
	}
}