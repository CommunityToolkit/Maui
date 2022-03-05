using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Platform;
using APaint = Android.Graphics.Paint;
using APath = Android.Graphics.Path;
using AView = Android.Views.View;
using Point = Microsoft.Maui.Graphics.Point;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public class DrawingNativeView : AView
{
	readonly APaint canvasPaint;
	readonly APaint drawPaint;
	readonly APath drawPath;
	readonly IDrawingView virtualView;
	Bitmap? canvasBitmap;
	Canvas? drawCanvas;
	ILine? currentLine;

	/// <summary>
	/// Initialize a new instance of <see cref="DrawingNativeView" />.
	/// </summary>
	public DrawingNativeView(IDrawingView virtualView, Context? context) : base(context)
	{
		this.virtualView = virtualView;
		drawPath = new APath();
		drawPaint = new APaint
		{
			AntiAlias = true
		};

		drawPaint.SetStyle(APaint.Style.Stroke);
		drawPaint.StrokeJoin = APaint.Join.Round;
		drawPaint.StrokeCap = APaint.Cap.Round;

		canvasPaint = new APaint
		{
			Dither = true
		};
		drawPaint.Color = this.virtualView.DefaultLineColor.ToNative();
		drawPaint.StrokeWidth = this.virtualView.DefaultLineWidth;
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		virtualView.Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadLines();

	/// <inheritdoc />
	protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
	{
		const int minW = 1;
		const int minH = 1;
		w = w < minW ? minW : w;
		h = h < minH ? minH : h;

		base.OnSizeChanged(w, h, oldw, oldh);

		canvasBitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888 ?? throw new NullReferenceException("Unable to create Bitmap config"));
		if (canvasBitmap is not null)
		{
			drawCanvas = new Canvas(canvasBitmap);
			LoadLines();
		}
	}

	/// <inheritdoc />
	protected override void OnDraw(Canvas? canvas)
	{
		base.OnDraw(canvas);

		if (canvas is not null && canvasBitmap is not null)
		{
			Draw(virtualView.Lines, canvas);

			canvas.DrawBitmap(canvasBitmap, 0, 0, canvasPaint);
			canvas.DrawPath(drawPath, drawPaint);
		}
	}

	/// <inheritdoc />
	public override bool OnTouchEvent(MotionEvent? e)
	{
		ArgumentNullException.ThrowIfNull(e);
		var touchX = e.GetX();
		var touchY = e.GetY();

		switch (e.Action)
		{
			case MotionEventActions.Down:
				Parent?.RequestDisallowInterceptTouchEvent(true);
				if (!virtualView.MultiLineMode)
				{
					virtualView.Lines.Clear();
				}

				currentLine = new Line()
				{
					Points = new System.Collections.ObjectModel.ObservableCollection<Point>()
						{
							new (touchX, touchY)
						}
				};

				drawCanvas?.DrawColor(GetBackgroundColor(), PorterDuff.Mode.Clear!);
				drawPath.MoveTo(touchX, touchY);
				break;
			case MotionEventActions.Move:
				if (touchX > 0 && touchY > 0 && touchX < drawCanvas?.Width && touchY < drawCanvas?.Height)
				{
					drawPath.LineTo(touchX, touchY);
				}

				currentLine?.Points.Add(new Point(touchX, touchY));
				break;
			case MotionEventActions.Up:
				Parent?.RequestDisallowInterceptTouchEvent(false);
				drawCanvas?.DrawPath(drawPath, drawPaint);
				drawPath.Reset();
				if (currentLine != null)
				{
					virtualView.Lines.Add(currentLine);
					OnDrawingLineCompleted(currentLine);
				}

				if (virtualView.ClearOnFinish)
				{
					virtualView.Lines.Clear();
				}

				break;
			default:
				return false;
		}

		Invalidate();

		return true;
	}

	IList<Point> NormalizePoints(IEnumerable<Point> points)
	{
		var newPoints = new List<Point>();
		foreach (var point in points)
		{
			var pointX = point.X;
			var pointY = point.Y;
			if (pointX < 0)
			{
				pointX = 0;
			}

			if (pointX > drawCanvas?.Width)
			{
				pointX = drawCanvas?.Width ?? 0;
			}

			if (point.Y < 0)
			{
				pointY = 0;
			}

			if (pointY > drawCanvas?.Height)
			{
				pointY = drawCanvas?.Height ?? 0;
			}

			newPoints.Add(new Point(pointX, pointY));
		}

		return newPoints;
	}

	void LoadLines()
	{
		if (drawCanvas is null)
		{
			return;
		}

		drawCanvas.DrawColor(GetBackgroundColor());
		drawPath.Reset();
		var lines = virtualView.Lines;
		Draw(lines, drawCanvas, drawPath);

		Invalidate();
	}

	void Draw(IEnumerable<ILine> lines, in Canvas canvas, APath? path = null)
	{
		foreach (var line in lines)
		{
			path ??= new APath();
			var points = NormalizePoints(line.Points);
			path.MoveTo((float)points[0].X, (float)points[0].Y);
			foreach (var (x, y) in points)
			{
				var pointX = (float)x;
				var pointY = (float)y;

				path.LineTo(pointX, pointY);
			}

			canvas.DrawPath(path, drawPaint);
			path.Reset();
		}
	}
	
	/// <summary>
	/// Clean up resources
	/// </summary>
	public void CleanUp()
	{
		drawCanvas?.Dispose();
		drawPaint.Dispose();
		drawPath.Dispose();
		canvasBitmap?.Dispose();
		canvasPaint.Dispose();
		virtualView.Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	Android.Graphics.Color GetBackgroundColor()
	{
		var background = virtualView.Background?.BackgroundColor ?? Colors.White;
		return background.ToNative();
	}

	void OnDrawingLineCompleted(ILine? lastDrawingLine)
	{
		if (virtualView.DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			virtualView.DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}