using System.Collections.ObjectModel;
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Platform;
using UIKit;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	readonly List<UIScrollView> scrollViewParents = new();

	/// <summary>
	/// Line color
	/// </summary>
	public UIColor LineColor { get; set; } = UIColor.Black;
	
	/// <inheritdoc />
	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		base.TouchesBegan(touches, evt);
		//DetectScrollViews();
		//SetParentTouches(false);

		Lines.CollectionChanged -= OnLinesCollectionChanged;

		if (!MultiLineMode)
		{
			Lines.Clear();
			currentPath = new();
		}

		var touch = (UITouch)touches.AnyObject;
		previousPoint = touch.PreviousLocationInView(this);
		currentPath.MoveTo((float)previousPoint.X, (float)previousPoint.Y);
		currentLine = new MauiDrawingLine
		{
			Points = new ObservableCollection<CGPoint>()
			{
				new (previousPoint.X.Value, previousPoint.Y.Value)
			}
		};

		Invalidate();

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	public override void TouchesMoved(NSSet touches, UIEvent? evt)
	{
		base.TouchesMoved(touches, evt);
		var touch = (UITouch)touches.AnyObject;
		var currentPoint = touch.LocationInView(this);

		AddPointToPath(currentPoint);
		Invalidate();

		currentLine?.Points.Add(currentPoint.ToPoint());
	}

	/// <inheritdoc />
	public override void TouchesEnded(NSSet touches, UIEvent? evt)
	{
		base.TouchesEnded(touches, evt);
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
		//SetParentTouches(true);
	}

	/// <inheritdoc />
	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		base.TouchesCancelled(touches, evt);
		currentLine = null;
		InvalidateDrawable();
		//SetParentTouches(true);
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

	void Invalidate()
	{
		SetNeedsDisplay();
	}
}