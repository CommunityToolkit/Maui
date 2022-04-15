using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
#if ANDROID
using PlatformPoint = Android.Graphics.PointF;
#elif IOS || MACCATALYST
using PlatformPoint = CoreGraphics.CGPoint;
#elif WINDOWS
using Microsoft.Maui.Graphics.Win2D;
using PlatformPoint = Windows.Foundation.Point;
#endif

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Platform Control
/// </summary>
public partial class MauiDrawingView
{
	readonly WeakEventManager weakEventManager = new();

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
	public bool MultiLineMode { get; set; }
	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ClearOnFinish { get; set; }

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; } = 5;

	void OnDrawingLineCompleted(MauiDrawingLine lastDrawingLine)
	{
		weakEventManager.HandleEvent(this, new MauiDrawingLineCompletedEventArgs(lastDrawingLine), nameof(DrawingLineCompleted));
	}

	/// <summary>
	/// Used to draw any shape on the canvas
	/// </summary>
	public Action<ICanvas, RectF>? Draw;

#if __MOBILE__ || WINDOWS

	PlatformPoint previousPoint;
	PathF currentPath = new();
	MauiDrawingLine? currentLine;

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
#if __MOBILE__
		Drawable = new DrawingViewDrawable(this);
#elif WINDOWS
		((W2DGraphicsView)Content).Drawable = new DrawingViewDrawable(this);
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

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
		LoadPoints();

	void AddPointToPath(PlatformPoint currentPoint)
	{
		var curPoint = new PointF((float)currentPoint.X, (float)currentPoint.Y);
		currentPath.LineTo(curPoint);
	}

	void LoadPoints()
	{
		ClearPath();
		foreach (var line in Lines)
		{
			var newPoints = line.EnableSmoothedPath
				? line.Points.SmoothedPathWithGranularity(line.Granularity)
				: line.Points;
#if ANDROID
			newPoints = NormalizePoints(newPoints);
#endif
			var stylusPoints = newPoints.Select(point => new PlatformPoint(point.X, point.Y)).ToList();
			if (stylusPoints.Count > 0)
			{
				previousPoint = stylusPoints[0];
				currentPath.MoveTo((float)previousPoint.X, (float)previousPoint.Y);
				foreach (var point in stylusPoints)
				{
					AddPointToPath(point);
				}
			}
		}

		Redraw();
	}

	void ClearPath()
	{
		currentPath = new PathF();
	}

	void Redraw()
	{
		Invalidate();
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
			drawingView.Draw?.Invoke(canvas, dirtyRect);
			canvas.StrokeColor = drawingView.LineColor.ToColor();
			canvas.StrokeSize = drawingView.LineWidth;
			canvas.DrawPath(drawingView.currentPath);
		}
	}
#endif
}