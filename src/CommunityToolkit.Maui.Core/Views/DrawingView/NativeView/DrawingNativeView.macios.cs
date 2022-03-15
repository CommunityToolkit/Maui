using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Extensions;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public class DrawingNativeView : UIView
{
	readonly UIBezierPath currentPath;
	CGPoint previousPoint;
	DrawingNativeLine? currentLine;
	readonly List<UIScrollView> scrollViewParents = new();

	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<DrawingNativeLineCompletedEventArgs>? DrawingLineCompleted;

	/// <summary>
	/// Line color
	/// </summary>
	public UIColor LineColor { get; set; }

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; }

	/// <summary>
	/// Command executed when drawing line completed
	/// </summary>
	public ICommand? DrawingLineCompletedCommand { get; set; }

	/// <summary>
	/// Drawing Lines
	/// </summary>
	public ObservableCollection<DrawingNativeLine> Lines { get; }

	/// <summary>
	/// Enable or disable multiline mode
	/// </summary>
	public bool MultiLineMode { get; set; }

	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ClearOnFinish { get; set; }

	/// <summary>
	/// Initialize a new instance of <see cref="DrawingNativeView" />.
	/// </summary>
	public DrawingNativeView()
	{
		LineColor = Colors.Black.ToNative();
		LineWidth = 5;
		Lines = new ObservableCollection<DrawingNativeLine>();

		currentPath = new UIBezierPath();
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadPoints();

	/// <inheritdoc />
	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		DetectScrollViews();
		SetParentTouches(false);

		Lines.CollectionChanged -= OnLinesCollectionChanged;
		if (!MultiLineMode)
		{
			Lines.Clear();
			currentPath.RemoveAllPoints();
		}

		var touch = (UITouch)touches.AnyObject;
		previousPoint = touch.PreviousLocationInView(this);
		currentPath.MoveTo(previousPoint);
		currentLine = new DrawingNativeLine
		{
			Points = new ObservableCollection<Point>()
			{
				new (previousPoint.X, previousPoint.Y)
			}
		};

		SetNeedsDisplay();

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	public override void TouchesMoved(NSSet touches, UIEvent? evt)
	{
		var touch = (UITouch)touches.AnyObject;
		var currentPoint = touch.LocationInView(this);
		AddPointToPath(currentPoint);
		SetNeedsDisplay();
		currentLine?.Points.Add(currentPoint.ToPoint());
	}

	/// <inheritdoc />
	public override void TouchesEnded(NSSet touches, UIEvent? evt)
	{
		if (currentLine != null)
		{
			UpdatePath(currentLine);
			Lines.Add(currentLine);
			OnDrawingLineCompleted(currentLine);
		}

		if (ClearOnFinish)
		{
			Lines.Clear();
		}

		currentLine = null;
		SetParentTouches(true);
	}

	/// <inheritdoc />
	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		currentLine = null;
		SetNeedsDisplay();
		SetParentTouches(true);
	}

	/// <inheritdoc />
	public override void Draw(CGRect rect)
	{
		currentPath.LineWidth = LineWidth;
		LineColor.SetStroke();
		currentPath.Stroke();
	}

	void AddPointToPath(CGPoint currentPoint) => currentPath.AddLineTo(currentPoint);

	void LoadPoints()
	{
		currentPath.RemoveAllPoints();
		foreach (var line in Lines)
		{
			var newPointsPath = line.EnableSmoothedPath
					? line.Points.SmoothedPathWithGranularity(line.Granularity)
					: line.Points;
			var stylusPoints = newPointsPath.Select(point => new CGPoint(point.X, point.Y)).ToList();
			if (stylusPoints.Count > 0)
			{
				previousPoint = stylusPoints[0];
				currentPath.MoveTo(previousPoint);
				foreach (var point in stylusPoints)
				{
					AddPointToPath(point);
				}
			}
		}

		SetNeedsDisplay();
	}

	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
		currentPath.Dispose();
		Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	void DetectScrollViews()
	{
		if (scrollViewParentRenderers.Any())
			return;

		var parent = Superview;

		while (parent != null)
		{
			if (parent.GetType() == typeof(UIScrollView))
			{
				scrollViewParents.Add((UIScrollView)parent);
			}

			parent = parent.Superview;
		}
	}

	void SetParentTouches(bool enabled)
	{
		foreach (var scrollViewParent in scrollViewParents)
		{
			scrollViewParent.ScrollEnabled = enabled;
		}
	}

	/// <summary>
	/// Executes DrawingLineCompleted event and DrawingLineCompletedCommand
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	void OnDrawingLineCompleted(DrawingNativeLine? lastDrawingLine)
	{
		DrawingLineCompleted?.Invoke(this, new DrawingNativeLineCompletedEventArgs(lastDrawingLine));
		if (DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}