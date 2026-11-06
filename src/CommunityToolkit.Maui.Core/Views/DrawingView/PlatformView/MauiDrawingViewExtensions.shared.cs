using System.Collections.Immutable;
using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.Core.Extensions;

/// <summary>
/// Extension methods to support <see cref="IDrawingView"/>
/// </summary>
public static class MauiDrawingViewExtensions
{
	/// <summary>
	/// Get smoothed path.
	/// </summary>
	/// <param name="currentPoints">The points to smooth.</param>
	/// <param name="granularity">The number of points to add between two points.</param>
	/// <returns>The smoothed points.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="currentPoints"/> is null.</exception>
	public static ObservableCollection<PointF> CreateSmoothedPathWithGranularity(this IEnumerable<PointF> currentPoints, int granularity)
	{
		ArgumentNullException.ThrowIfNull(currentPoints);

		var currentPointsList = new List<PointF>(currentPoints);

		// not enough points to smooth effectively, so return the original path and points.
		if (currentPointsList.Count < granularity + 2)
		{
			return currentPointsList.ToObservableCollection();
		}

		var smoothedPointsList = new List<PointF>();

		// duplicate the first and last points as control points.
		currentPointsList.Insert(0, currentPointsList[0]);
		currentPointsList.Add(currentPointsList[^1]);

		// add the first point
		smoothedPointsList.Add(currentPointsList[0]);

		var currentPointsCount = currentPointsList.Count;
		for (var index = 1; index < currentPointsCount - 2; index++)
		{
			var p0 = currentPointsList[index - 1];
			var p1 = currentPointsList[index];
			var p2 = currentPointsList[index + 1];
			var p3 = currentPointsList[index + 2];

			// add n points starting at p1 + dx/dy up until p2 using Catmull-Rom splines
			for (var i = 1; i < granularity; i++)
			{
				var t = i * (1f / granularity);
				var tt = t * t;
				var ttt = tt * t;

				// intermediate point
				var mid = GetIntermediatePoint(p0, p1, p2, p3, t, tt, ttt);
				smoothedPointsList.Add(mid);
			}

			// add p2
			smoothedPointsList.Add(p2);
		}

		// add the last point
		var last = currentPointsList[^1];
		smoothedPointsList.Add(last);

		return smoothedPointsList.ToObservableCollection();
	}

	/// <summary>
	/// Set MultiLine mode
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="multiLineMode">value</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> is null.</exception>
	public static void SetIsMultiLineModeEnabled(this MauiDrawingView mauiDrawingView, bool multiLineMode)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);

		mauiDrawingView.IsMultiLineModeEnabled = multiLineMode;
	}

	/// <summary>
	/// Set DrawAction action
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="draw">value</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> is null.</exception>
	public static void SetDrawAction(this MauiDrawingView mauiDrawingView, Action<ICanvas, RectF>? draw)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);

		mauiDrawingView.DrawAction = draw;
	}

	/// <summary>
	/// Set ClearOnFinish
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="clearOnFinish">value</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> is null.</exception>
	public static void SetShouldClearOnFinish(this MauiDrawingView mauiDrawingView, bool clearOnFinish)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);

		mauiDrawingView.ShouldClearOnFinish = clearOnFinish;
	}

	/// <summary>
	/// Set LineColor
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="lineColor">line color</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> or <paramref name="lineColor"/> is null.</exception>
	public static void SetLineColor(this MauiDrawingView mauiDrawingView, Color lineColor)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);
		ArgumentNullException.ThrowIfNull(lineColor);

		mauiDrawingView.LineColor = lineColor;
	}

	/// <summary>
	/// Set LineWidth
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="lineWidth">line width</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> is null.</exception>
	public static void SetLineWidth(this MauiDrawingView mauiDrawingView, float lineWidth)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);

		mauiDrawingView.LineWidth = lineWidth;
	}

	/// <summary>
	/// Set Paint
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/></param>
	/// <param name="background">background</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> or <paramref name="background"/> is null.</exception>
	public static void SetPaint(this MauiDrawingView mauiDrawingView, Paint background)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);
		ArgumentNullException.ThrowIfNull(background);

		mauiDrawingView.Paint = background;
	}

	/// <summary>
	/// Set Lines
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/><see cref="MauiDrawingView"/></param>
	/// <param name="drawingView"><see cref="IDrawingView"/></param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="mauiDrawingView"/> or <paramref name="drawingView"/> is null.</exception>
	public static void SetLines(this MauiDrawingView mauiDrawingView, IDrawingView drawingView)
	{
		ArgumentNullException.ThrowIfNull(mauiDrawingView);
		ArgumentNullException.ThrowIfNull(drawingView);

		var lines = drawingView.Lines.ToImmutableList();
		if (!drawingView.IsMultiLineModeEnabled && lines.Count > 1)
		{
			lines = lines.TakeLast(1).ToImmutableList();
		}

		mauiDrawingView.Lines.Clear();

		foreach (var line in lines)
		{
			mauiDrawingView.Lines.Add(new MauiDrawingLine
			{
				LineColor = line.LineColor,
				ShouldSmoothPathWhenDrawn = line.ShouldSmoothPathWhenDrawn,
				Granularity = line.Granularity,
				LineWidth = line.LineWidth,
				Points = line.Points.ToObservableCollection()
			});
		}
	}

	/// <summary>
	/// Set Lines
	/// </summary>
	/// <param name="mauiDrawingView"><see cref="MauiDrawingView"/><see cref="MauiDrawingView"/></param>
	/// <param name="drawingView"><see cref="IDrawingView"/></param>
	/// <param name="adapter"><see cref="IDrawingLineAdapter"/></param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="drawingView"/>, <paramref name="mauiDrawingView"/> or <paramref name="adapter"/> is null.</exception>
	public static void SetLines(this IDrawingView drawingView, MauiDrawingView mauiDrawingView, IDrawingLineAdapter adapter)
	{
		ArgumentNullException.ThrowIfNull(drawingView);
		ArgumentNullException.ThrowIfNull(mauiDrawingView);
		ArgumentNullException.ThrowIfNull(adapter);

		var lines = mauiDrawingView.Lines.ToImmutableList();
		if (!mauiDrawingView.IsMultiLineModeEnabled && lines.Count > 1)
		{
			lines = lines.TakeLast(1).ToImmutableList();
		}

		drawingView.Lines.Clear();

		foreach (var line in lines)
		{
			drawingView.Lines.Add(adapter.ConvertMauiDrawingLine(line));
		}
	}

	static Point GetIntermediatePoint(PointF p0, PointF p1, PointF p2, PointF p3, in float t, in float tt, in float ttt) => new()
	{
		X = 0.5f *
		(2f * p1.X +
			(p2.X - p0.X) * t +
			(2f * p0.X - 5f * p1.X + 4f * p2.X - p3.X) * tt +
			(3f * p1.X - p0.X - 3f * p2.X + p3.X) * ttt),
		Y = 0.5f *
		(2 * p1.Y +
			(p2.Y - p0.Y) * t +
			(2 * p0.Y - 5 * p1.Y + 4 * p2.Y - p3.Y) * tt +
			(3 * p1.Y - p0.Y - 3 * p2.Y + p3.Y) * ttt)
	};
}