using System.Collections.ObjectModel;
using Android.Content;
using Android.Views;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using AColor = Android.Graphics.Color;
using APaint = Android.Graphics.Paint;
using APath = Android.Graphics.Path;
using APoint = Android.Graphics.PointF;
using AView = Android.Views.View;
using Point = Microsoft.Maui.Graphics.Point;

namespace CommunityToolkit.Maui.Core.Views;

public partial class MauiDrawingView : PlatformTouchGraphicsView
{
	/// <summary>
	/// Line color
	/// </summary>
	public AColor LineColor { get; set; } = AColor.Black;

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

		switch (e.Action)
		{
			case MotionEventActions.Down:
				Parent?.RequestDisallowInterceptTouchEvent(true);
				if (!MultiLineMode)
				{
					Lines.Clear();
				}

				currentLine = new MauiDrawingLine()
				{
					Points = new ObservableCollection<APoint>
						{
							new (touchX, touchY)
						}
				};

				currentPath.MoveTo(touchX, touchY);
				break;

			case MotionEventActions.Move:
				if (touchX > 0 && touchY > 0 && touchX < Width && touchY < Height)
				{
					currentPath.LineTo(touchX, touchY);
				}

				currentLine?.Points.Add(new Point(touchX, touchY));
				break;

			case MotionEventActions.Up:
				Parent?.RequestDisallowInterceptTouchEvent(false);
				currentPath = new();
				if (currentLine != null)
				{
					Lines.Add(currentLine);
					OnDrawingLineCompleted(currentLine);
				}

				if (ClearOnFinish)
				{
					Lines.Clear();
				}

				currentLine = null;
				break;

			default:
				return false;
		}

		Invalidate();

		return true;
	}

	ObservableCollection<APoint> NormalizePoints(IEnumerable<APoint> points)
	{
		var newPoints = new List<APoint>();
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

			newPoints.Add(new Point(pointX, pointY));
		}

		return newPoints.ToObservableCollection();
	}
}