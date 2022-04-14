using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Views;
using Microsoft.Maui.Platform;
#if ANDROID
using Point = Android.Graphics.PointF;
#elif IOS || MACCATALYST
using Point = CoreGraphics.CGPoint;
#elif WINDOWS
using Point = Windows.Foundation.Point;
#endif

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IDrawingView"/>
/// </summary>
public static partial class MauiDrawingViewExtensions
{
	/// <summary>
	/// Get smoothed path.
	/// </summary>
	public static ObservableCollection<Point> SmoothedPathWithGranularity(this IEnumerable<Point> currentPoints, int granularity)
	{
		var currentPointsCopy = new List<Point>(currentPoints);

		// not enough points to smooth effectively, so return the original path and points.
		if (currentPointsCopy.Count < granularity + 2)
		{
			return new(currentPointsCopy);
		}

		var smoothedPoints = new List<Point>();

		// duplicate the first and last points as control points.
		currentPointsCopy.Insert(0, currentPointsCopy[0]);
		currentPointsCopy.Add(currentPointsCopy[^1]);

		// add the first point
		smoothedPoints.Add(currentPointsCopy[0]);

		var currentPointsCount = currentPointsCopy.Count;
		for (var index = 1; index < currentPointsCount - 2; index++)
		{
			var p0 = currentPointsCopy[index - 1];
			var p1 = currentPointsCopy[index];
			var p2 = currentPointsCopy[index + 1];
			var p3 = currentPointsCopy[index + 2];

			// add n points starting at p1 + dx/dy up until p2 using Catmull-Rom splines
			for (var i = 1; i < granularity; i++)
			{
				var t = i * (1f / granularity);
				var tt = t * t;
				var ttt = tt * t;

				// intermediate point
				var mid = GetIntermediatePoint(p0, p1, p2, p3, t, tt, ttt);
				smoothedPoints.Add(mid);
			}

			// add p2
			smoothedPoints.Add(p2);
		}

		// add the last point
		var last = currentPointsCopy[^1];
		smoothedPoints.Add(last);

		return new(smoothedPoints);
	}

	static Point GetIntermediatePoint(Point p0, Point p1, Point p2, Point p3, in float t, in float tt, in float ttt) => new()
	{
		X = 0.5f *
			((2f * p1.X) +
				((p2.X - p0.X) * t) +
				(((2f * p0.X) - (5f * p1.X) + (4f * p2.X) - p3.X) * tt) +
				(((3f * p1.X) - p0.X - (3f * p2.X) + p3.X) * ttt)),
		Y = 0.5f *
			((2 * p1.Y) +
				((p2.Y - p0.Y) * t) +
				(((2 * p0.Y) - (5 * p1.Y) + (4 * p2.Y) - p3.Y) * tt) +
				(((3 * p1.Y) - p0.Y - (3 * p2.Y) + p3.Y) * ttt))
	};

#if (ANDROID || IOS || MACCATALYST || WINDOWS)
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
		if (!drawingView.MultiLineMode && lines.Count > 1)
		{
			lines = lines.TakeLast(1).ToList();
		}

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

		drawingView.Lines.Clear();

		foreach (var line in lines)
		{
			drawingView.Lines.Add(new DrawingLine()
			{
				LineColor = line.LineColor.ToColor() ?? Colors.Transparent,
				EnableSmoothedPath = line.EnableSmoothedPath,
				Granularity = line.Granularity,
				LineWidth = line.LineWidth,
				Points = line.Points.Select(p => new Microsoft.Maui.Graphics.Point(p.X, p.Y)).ToObservableCollection()
			});
		}
	}
#endif
}