﻿using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Platform Control
/// </summary>
public partial class MauiDrawingView
{
	readonly WeakEventManager weakEventManager = new();

	bool isDrawing;
	PointF previousPoint;
	PathF currentPath = new();
	MauiDrawingLine? currentLine;

	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<MauiDrawingLineCompletedEventArgs> DrawingLineCompleted
	{
		add => weakEventManager.AddEventHandler(value);
		remove => weakEventManager.RemoveEventHandler(value);
	}

	/// <summary>
	/// Drawing Lines
	/// </summary>
	public ObservableCollection<MauiDrawingLine> Lines { get; } = new();

	/// <summary>
	/// Enable or disable multiline mode
	/// </summary>
	public bool IsMultiLineModeEnabled { get; set; } = DrawingViewDefaults.IsMultiLineModeEnabled;

	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ShouldClearOnFinish { get; set; } = DrawingViewDefaults.ShouldClearOnFinish;

	/// <summary>
	/// Line color
	/// </summary>
	public Color LineColor { get; set; } = DrawingViewDefaults.LineColor;

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; } = DrawingViewDefaults.LineWidth;

	/// <summary>
	/// Used to draw any shape on the canvas
	/// </summary>
	public Action<ICanvas, RectF>? DrawAction { get; set; }

	/// <summary>
	/// Drawable background
	/// </summary>
	public Paint Paint { get; set; } = new SolidPaint(DrawingViewDefaults.BackgroundColor);

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
#if ANDROID || IOS || MACCATALYST
		Drawable = new DrawingViewDrawable(this);
#elif WINDOWS
		((Microsoft.Maui.Graphics.Win2D.W2DGraphicsView)Content).Drawable = new DrawingViewDrawable(this);
#endif
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
		currentPath.Dispose();
		Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	void OnStart(PointF point)
	{
		isDrawing = true;

		Lines.CollectionChanged -= OnLinesCollectionChanged;

		if (!IsMultiLineModeEnabled)
		{
			Lines.Clear();
			ClearPath();
		}

		previousPoint = point;
		currentPath.MoveTo(previousPoint.X, previousPoint.Y);
		currentLine = new MauiDrawingLine
		{
			Points = new ObservableCollection<PointF>
			{
				new(previousPoint.X, previousPoint.Y)
			},
			LineColor = LineColor,
			LineWidth = LineWidth
		};

		Redraw();

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	void OnMoving(PointF currentPoint)
	{
		if (!isDrawing)
		{
			return;
		}

#if !ANDROID
		AddPointToPath(currentPoint);
#endif

		Redraw();
		currentLine?.Points.Add(currentPoint);
	}

	void OnFinish()
	{
		if (currentLine is not null)
		{
			Lines.Add(currentLine);
			OnDrawingLineCompleted(currentLine);
		}

		if (ShouldClearOnFinish)
		{
			Lines.Clear();
			ClearPath();
		}

		currentLine = null;
		isDrawing = false;
	}

	void OnCancel()
	{
		currentLine = null;
		ClearPath();
		Redraw();
		isDrawing = false;
	}

	void OnDrawingLineCompleted(MauiDrawingLine lastDrawingLine) =>
		weakEventManager.HandleEvent(this, new MauiDrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadLines();

	void AddPointToPath(PointF currentPoint) => currentPath.LineTo(currentPoint);

	void LoadLines()
	{
		ClearPath();
		Redraw();
	}

	void ClearPath()
	{
		currentPath = new PathF();
	}

	void Redraw()
	{
#if IOS || ANDROID || MACCATALYST || WINDOWS
		Invalidate();
#endif
	}

	class DrawingViewDrawable : IDrawable
	{
		readonly MauiDrawingView drawingView;

		public DrawingViewDrawable(MauiDrawingView drawingView)
		{
			this.drawingView = drawingView;
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.SetFillPaint(drawingView.Paint, dirtyRect);
			canvas.FillRectangle(dirtyRect);

			drawingView.DrawAction?.Invoke(canvas, dirtyRect);

			DrawCurrentLines(canvas, drawingView);
			SetStroke(canvas, drawingView.LineWidth, drawingView.LineColor);

			canvas.DrawPath(drawingView.currentPath);
		}

		static void SetStroke(in ICanvas canvas, in float lineWidth, in Color lineColor)
		{
			canvas.StrokeColor = lineColor;
			canvas.StrokeSize = lineWidth;
		}

		static void DrawCurrentLines(in ICanvas canvas, in MauiDrawingView drawingView)
		{
			foreach (var line in drawingView.Lines)
			{
				var path = new PathF();
				var points = line.ShouldSmoothPathWhenDrawn
					? line.Points.CreateSmoothedPathWithGranularity(line.Granularity)
					: line.Points;
#if ANDROID
				points = CreateCollectionWithNormalizedPoints(points, drawingView.Width, drawingView.Height);
#endif
				if (points.Count > 0)
				{
					path.MoveTo(points[0].X, points[0].Y);
					foreach (var point in points)
					{
						path.LineTo(point);
					}

					SetStroke(canvas, line.LineWidth, line.LineColor);
					canvas.DrawPath(path);
				}
			}
		}

#if ANDROID
		static ObservableCollection<PointF> CreateCollectionWithNormalizedPoints(in ObservableCollection<PointF> points, in int drawingViewWidth, in int drawingViewHeight)
		{
			var newPoints = new List<PointF>();
			foreach (var point in points)
			{
				var pointX = Math.Clamp(point.X, 0, drawingViewWidth);
				var pointY = Math.Clamp(point.Y, 0, drawingViewHeight);

				newPoints.Add(new PointF(pointX, pointY));
			}

			return newPoints.ToObservableCollection();
		}
#endif
	}
}