using Tizen.UIExtensions.NUI.GraphicsView;
using PointStateType = Tizen.NUI.PointStateType;
using DeviceInfo = Tizen.UIExtensions.Common.DeviceInfo;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// The Tizen platform-specific view that handles touch input for drawing operations.
/// </summary>
public class PlatformTouchGraphicsView : SkiaGraphicsView
{
	IGraphicsView? graphicsView;
	RectF bounds;
	bool dragStarted;
	PointF[] lastMovedViewPoints = Array.Empty<PointF>();
	bool pressedContained;

	/// <summary>
	/// Initialize a new instance of the class
	/// </summary>
	/// <param name="drawable"></param>
	public PlatformTouchGraphicsView(IDrawable? drawable = null) : base(drawable)
	{
		HoverEvent += OnHoverEvent;
		TouchEvent += OnTouchEvent;
	}

	/// <summary>
	/// Handles the view's resize events.
	/// </summary>
	protected override void OnResized()
	{
		base.OnResized();
		bounds = new RectF(0, 0, (float)(SizeWidth / DeviceInfo.ScalingFactor), (float)(SizeHeight / DeviceInfo.ScalingFactor));
	}

	/// <summary>
	/// Connects this platform view to the cross-platform IGraphicsView interface.
	/// </summary>
	/// <param name="graphicsView"></param>
	public void Connect(IGraphicsView graphicsView) => this.graphicsView = graphicsView;

	/// <summary>
	/// Disconnects from the IGraphicsView.
	/// </summary>
	public void Disconnect() => graphicsView = null;

	bool OnTouchEvent(object source, TouchEventArgs e)
	{
		int touchCount = (int)e.Touch.GetPointCount();
		var touchPoints = new PointF[touchCount];
		for (uint i = 0; i < touchCount; i++)
		{
			touchPoints[i] = new PointF((float)(e.Touch.GetLocalPosition(i).X / DeviceInfo.ScalingFactor), (float)(e.Touch.GetLocalPosition(i).Y / DeviceInfo.ScalingFactor));
		}

		switch (e.Touch.GetState(0))
		{
			case PointStateType.Motion:
				TouchesMoved(touchPoints);
				break;
			case PointStateType.Down:
				TouchesBegan(touchPoints);
				break;
			case PointStateType.Up:
				TouchesEnded(touchPoints);
				break;
			case PointStateType.Interrupted:
				TouchesCanceled();
				break;
		}

		return false;
	}

	bool OnHoverEvent(object source, HoverEventArgs e)
	{
		int touchCount = (int)e.Hover.GetPointCount();
		var touchPoints = new PointF[touchCount];
		for (uint i = 0; i < touchCount; i++)
		{
			touchPoints[i] = new PointF((float)(e.Hover.GetLocalPosition(i).X / DeviceInfo.ScalingFactor), (float)(e.Hover.GetLocalPosition(i).Y / DeviceInfo.ScalingFactor));
		}

		switch (e.Hover.GetState(0))
		{
			case PointStateType.Motion:
				graphicsView?.MoveHoverInteraction(touchPoints);
				break;
			case PointStateType.Started:
				graphicsView?.StartHoverInteraction(touchPoints);
				break;
			case PointStateType.Finished:
				graphicsView?.EndHoverInteraction();
				break;
		}

		return false;
	}

	void TouchesBegan(PointF[] points)
	{
		dragStarted = false;
		lastMovedViewPoints = points;
		graphicsView?.StartInteraction(points);
		pressedContained = true;
	}

	void TouchesMoved(PointF[] points)
	{
		if (!dragStarted)
		{
			if (points.Length == 1)
			{
				float deltaX = lastMovedViewPoints[0].X - points[0].X;
				float deltaY = lastMovedViewPoints[0].Y - points[0].Y;

				if (MathF.Abs(deltaX) <= 3 && MathF.Abs(deltaY) <= 3)
				{
					return;
				}
			}
		}

		lastMovedViewPoints = points;
		dragStarted = true;
		pressedContained = bounds.ContainsAny(points);
		graphicsView?.DragInteraction(points);
	}

	void TouchesEnded(PointF[] points)
	{
		graphicsView?.EndInteraction(points, pressedContained);
	}

	void TouchesCanceled()
	{
		pressedContained = false;
		graphicsView?.CancelInteraction();
	}
}