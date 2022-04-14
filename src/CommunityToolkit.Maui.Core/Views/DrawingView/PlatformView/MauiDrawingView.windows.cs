using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Graphics.Win2D;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Input;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WPoint = Windows.Foundation.Point;
using WColor = Microsoft.UI.Colors;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	PathF currentPath = new();
	WPoint previousPoint;
	MauiDrawingLine? currentLine;
	bool isDrawing;
	
	/// <summary>
	/// Line color
	/// </summary>
	public WBrush LineColor { get; set; } = new WSolidColorBrush(WColor.Black);

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; } = 5;

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		isDrawing = true;
		base.OnPointerPressed(e);

		Lines.CollectionChanged -= OnLinesCollectionChanged;

		if (!MultiLineMode)
		{
			Lines.Clear();
			ClearPath();
		}

		previousPoint = e.GetCurrentPoint(this).Position;
		currentPath.MoveTo(previousPoint._x, previousPoint._y);
		currentLine = new MauiDrawingLine
		{
			Points = new ObservableCollection<WPoint>
			{
				new(previousPoint.X, previousPoint.Y)
			}
		};

		Redraw();

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		if (!isDrawing)
		{
			return;
		}
		
		var currentPoint = e.GetCurrentPoint(this).Position;
		AddPointToPath(currentPoint);
		Redraw();

		currentLine?.Points.Add(currentPoint);
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerRoutedEventArgs e)
	{
		base.OnPointerReleased(e);
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

	/// <inheritdoc />
	protected override void OnPointerCanceled(PointerRoutedEventArgs e)
	{
		base.OnPointerCanceled(e);
		currentLine = null;
		ClearPath();
		Redraw();
		isDrawing = false;
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		((W2DGraphicsView)Content).Drawable = new WindowsDrawingViewDrawable(this);
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

	void AddPointToPath(WPoint currentPoint)
	{
		var curPoint = new PointF((float) currentPoint.X, (float) currentPoint.Y);
		currentPath.LineTo(curPoint);
	}

	void LoadPoints()
	{
		ClearPath();
		foreach (var line in Lines)
		{
			var newPointsPath = line.EnableSmoothedPath
				? line.Points.SmoothedPathWithGranularity(line.Granularity)
				: line.Points;
			var stylusPoints = newPointsPath.Select(point => new WPoint(point.X, point.Y)).ToList();
			if (stylusPoints.Count > 0)
			{
				previousPoint = stylusPoints[0];
				currentPath.MoveTo(previousPoint._x, previousPoint._y);
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
	
	class WindowsDrawingViewDrawable : IDrawable
	{
		readonly MauiDrawingView drawingView;

		public WindowsDrawingViewDrawable(MauiDrawingView drawingView)
		{
			this.drawingView = drawingView;
		}
		
		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.FillColor = drawingView.Background.ToColor();
			canvas.StrokeColor = drawingView.LineColor.ToColor();
			canvas.StrokeSize = drawingView.LineWidth;
			canvas.DrawPath(drawingView.currentPath);
		}
	}
}