using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
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
	}

	/// <inheritdoc />
	public override void TouchesCancelled(NSSet touches, UIEvent? evt)
	{
		base.TouchesCancelled(touches, evt);
		OnCancel();
		SetParentTouches(true);
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