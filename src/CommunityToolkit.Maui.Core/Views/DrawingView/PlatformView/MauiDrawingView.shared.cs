using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;

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
	/// Line color
	/// </summary>
	public Color LineColor { get; set; } = Colors.Black;

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
	public Action<ICanvas, RectF>? DrawAction;

	/// <summary>
	/// Drawable background
	/// </summary>
	public Paint Paint { get; set; } = new SolidPaint(Defaults.BackgroundColor);

#if ANDROID || IOS || MACCATALYST || WINDOWS

	PointF previousPoint;
	PathF currentPath = new();
	MauiDrawingLine? currentLine;

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
	
	bool isDrawing;
	void OnStart(PointF point)
	{
		isDrawing = true;

		Lines.CollectionChanged -= OnLinesCollectionChanged;

		if (!MultiLineMode)
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

		if (ClearOnFinish)
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

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
		currentPath.Dispose();
		Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) =>
		LoadLines();

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
			canvas.SetFillPaint(drawingView.Paint, dirtyRect);
			canvas.FillRectangle(dirtyRect);
			drawingView.DrawAction?.Invoke(canvas, dirtyRect);
			DrawCurrentLines(canvas, drawingView.Lines);
			SetStroke(canvas, drawingView.LineWidth, drawingView.LineColor);
			canvas.DrawPath(drawingView.currentPath);
		}

		void SetStroke(ICanvas canvas, float lineWidth, Color lineColor)
		{
			canvas.StrokeColor = lineColor;
			canvas.StrokeSize = lineWidth;
		}

		void DrawCurrentLines(in ICanvas canvas, in IEnumerable<MauiDrawingLine> lines)
		{
			foreach (var line in lines)
			{
				var path = new PathF();
				var points = line.EnableSmoothedPath
					? line.Points.SmoothedPathWithGranularity(line.Granularity)
					: line.Points;
#if ANDROID
				points = NormalizePoints(points);
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
		ObservableCollection<PointF> NormalizePoints(ObservableCollection<PointF> points)
		{
			var newPoints = new ObservableCollection<PointF>();
			foreach (var point in points)
			{
				var pointX = point.X;
				var pointY = point.Y;
				if (pointX < 0)
				{
					pointX = 0;
				}

				if (pointX > drawingView.Width)
				{
					pointX = drawingView.Width;
				}

				if (point.Y < 0)
				{
					pointY = 0;
				}

				if (pointY > drawingView.Height)
				{
					pointY = drawingView.Height;
				}

				newPoints.Add(new PointF(pointX, pointY));
			}

			return newPoints;
		}
#endif
	}
#endif
	}