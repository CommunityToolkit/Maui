using System.Collections.ObjectModel;
using Android.Content;
using Android.Views;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using APaint = Android.Graphics.Paint;
using APath = Android.Graphics.Path;
using AView = Android.Views.View;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	/// <summary>
	/// Initialize a new instance of <see cref="MauiDrawingView" />.
	/// </summary>
	public MauiDrawingView(Context context) : base(context)
	{
		previousPoint = new();
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			currentPath.Dispose();
		}

		base.Dispose(disposing);
	}

	/// <inheritdoc />
	public override bool OnTouchEvent(MotionEvent? e)
	{
		base.OnTouchEvent(e);
		ArgumentNullException.ThrowIfNull(e);

		var touchX = e.GetX();
		var touchY = e.GetY();
		var point = new PointF(touchX / (float)DeviceDisplay.MainDisplayInfo.Density, touchY / (float)DeviceDisplay.MainDisplayInfo.Density);
		switch (e.Action)
		{
			case MotionEventActions.Down:
				Parent?.RequestDisallowInterceptTouchEvent(true);
				OnStart(point);
				break;

			case MotionEventActions.Move:
				if (touchX > 0 && touchY > 0 && touchX < Width && touchY < Height)
				{
					AddPointToPath(point);
				}

				OnMoving(point);
				break;

			case MotionEventActions.Up:
				Parent?.RequestDisallowInterceptTouchEvent(false);
				OnFinish();
				break;
			case MotionEventActions.Cancel:
				Parent?.RequestDisallowInterceptTouchEvent(false);
				OnCancel();
				break;

			default:
				return false;
		}

		Redraw();

		return true;
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Drawable = new DrawingViewDrawable(this);
		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	void Redraw()
	{
		Invalidate();
	}

	static ObservableCollection<PointF> CreateCollectionWithNormalizedPoints(in ObservableCollection<PointF> points, in int drawingViewWidth, in int drawingViewHeight, in float canvasScale)
	{
		var newPoints = new List<PointF>();
		foreach (var point in points)
		{
			var pointX = Math.Clamp(point.X, 0, drawingViewWidth / canvasScale);
			var pointY = Math.Clamp(point.Y, 0, drawingViewHeight / canvasScale);
			newPoints.Add(new PointF(pointX, pointY));
		}

		return newPoints.ToObservableCollection();
	}
}