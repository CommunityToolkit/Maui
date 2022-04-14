using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core.Extensions;
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
				new (previousPoint.X, previousPoint.Y)
			}
		};

		Invalidate();

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		var currentPoint = e.GetCurrentPoint(this).Position;
		AddPointToPath(currentPoint);
		Invalidate();
		
		currentLine?.Points.Add(currentPoint);
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerRoutedEventArgs e)
	{
		if (currentLine is not null)
		{
			Lines.Add(currentLine);
			OnDrawingLineCompleted(currentLine);
		}

		if (ClearOnFinish)
		{
			Lines.Clear();
		}

		currentLine = null;
	}

	/// <inheritdoc />
	protected override void OnPointerCanceled(PointerRoutedEventArgs e)
	{
		base.OnPointerCanceled(e);
		currentLine = null;
		Invalidate();
	}
	
	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
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

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadPoints();

	void AddPointToPath(WPoint currentPoint)
	{
		var curPoint = new PointF((float)currentPoint.X, (float)currentPoint.Y);
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
				AddPointToPath(previousPoint);
				foreach (var point in stylusPoints)
				{
					AddPointToPath(point);
				}
			}
		}

		currentPath.

		Invalidate();
	}

	void ClearPath()
	{
		currentPath = new PathF();
	}
}