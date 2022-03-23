using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CommunityToolkit.Maui.Core.Extensions;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : UIView
{
	readonly UIBezierPath currentPath = new();
	readonly List<UIScrollView> scrollViewParents = new();

	CGPoint previousPoint;
	MauiDrawingLine? currentLine;

	/// <summary>
	/// Line color
	/// </summary>
	public UIColor LineColor { get; set; } = UIColor.Black;

	/// <summary>
	/// Line width
	/// </summary>
	public NFloat LineWidth { get; set; } = 5;

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
		currentLine = new MauiDrawingLine
		{
			Points = new ObservableCollection<CGPoint>()
			{
				new (previousPoint.X.Value, previousPoint.Y.Value)
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

	void DetectScrollViews()
	{
		if (scrollViewParents.Count > 0)
		{
			return;
		}

		var parent = Superview;

		while (parent is not null)
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
}