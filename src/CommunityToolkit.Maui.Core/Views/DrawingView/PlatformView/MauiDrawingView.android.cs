using System.Collections.ObjectModel;
using Android.Content;
using Android.Views;
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
	public override bool OnTouchEvent(MotionEvent? e)
	{
		base.OnTouchEvent(e);
		ArgumentNullException.ThrowIfNull(e);

		var touchX = e.GetX();
		var touchY = e.GetY();
		var point = new PointF(touchX, touchY);
		switch (e.Action)
		{
			case MotionEventActions.Down:
				Parent?.RequestDisallowInterceptTouchEvent(true);
				OnStart(point);
				break;

			case MotionEventActions.Move:
				if (touchX > 0 && touchY > 0 && touchX < Width && touchY < Height)
				{
					AddPointToPath(currentPath, point);
				}
				
				OnMoving(new Point(touchX, touchY));
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

	ObservableCollection<PointF> NormalizePoints(IEnumerable<PointF> points)
	{
		var newPoints = new ObservableCollection<PointF>();
		foreach (var point in points)
		{
			var pointX = point.X;
			var pointY = point.Y;
			if (pointX < 0)
			{
				pointX = 0;
			}

			if (pointX > Width)
			{
				pointX = Width;
			}

			if (point.Y < 0)
			{
				pointY = 0;
			}

			if (pointY > Height)
			{
				pointY = Height;
			}

			newPoints.Add(new PointF(pointX, pointY));
		}

		return newPoints;
	}
}