using System.Collections.ObjectModel;
using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Input;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Microsoft.UI.Colors;
using WPoint = Windows.Foundation.Point;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	bool isDrawing;
	
	/// <summary>
	/// Line color
	/// </summary>
	public WBrush LineColor { get; set; } = new WSolidColorBrush(WColor.Black);

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
}