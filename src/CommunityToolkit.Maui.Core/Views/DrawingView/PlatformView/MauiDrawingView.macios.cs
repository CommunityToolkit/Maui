using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	// The scroll views are released when the gesture ends or is cancelled: kept longer, an ancestor (which retains this view as a subview) and
	// this view keep each other alive, and the list goes stale when this view moves into another UIScrollView.
	readonly List<UIScrollView> scrollViewParents = [];

	/// <inheritdoc />
	public override void TouchesBegan(NSSet touches, UIEvent? evt)
	{
		base.TouchesBegan(touches, evt);
		DetectScrollViews();
		SetParentTouches(false);

		var touch = (UITouch)touches.AnyObject;
		OnStart(touch.PreviousLocationInView(this).AsPointF());
	}

	/// <inheritdoc />
	public override void TouchesMoved(NSSet touches, UIEvent? evt)
	{
		base.TouchesMoved(touches, evt);
		var touch = (UITouch)touches.AnyObject;
		var currentPoint = touch.LocationInView(this);
		OnMoving(currentPoint.AsPointF());
	}

	/// <inheritdoc />
	public override void TouchesEnded(NSSet touches, UIEvent? evt)
	{
		base.TouchesEnded(touches, evt);
		OnFinish();
		SetParentTouches(true);
		scrollViewParents.Clear();
	}

	/// <inheritdoc />
	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		base.TouchesCancelled(touches, evt);
		OnCancel();
		SetParentTouches(true);
		scrollViewParents.Clear();
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Drawable = new DrawingViewDrawable(this);
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc/>
	public override void MovedToWindow()
	{
		base.MovedToWindow();

		// AttachedToWindow is raised after the move, as MAUI raises Loaded, so that its subscribers can change the view hierarchy
		if (Window is not null)
		{
			BeginInvokeOnMainThread(() =>
			{
				if (Window is not null)
				{
					RaiseAttachedToWindow();
				}
			});
		}
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			currentPath.Dispose();
		}

		base.Dispose(disposing);
	}

	void DetectScrollViews()
	{
		if (scrollViewParents.Count > 0)
		{
			// The gesture that found them has not ended yet
			return;
		}

		var parent = Superview;

		while (parent is not null)
		{
			if (parent is UIScrollView scrollView)
			{
				scrollViewParents.Add(scrollView);
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

	void Redraw()
	{
		Invalidate();
	}
}