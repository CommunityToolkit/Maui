using Android.Content;
using Android.Graphics;
using Android.Views;
using Microsoft.Maui.Platform;
using Paint = Android.Graphics.Paint;
using Path = Android.Graphics.Path;
using View = Android.Views.View;
using Point = Microsoft.Maui.Graphics.Point;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// 
/// </summary>
public class DrawingNativeView : View
{
	bool disposed;

	readonly Paint canvasPaint;
	readonly Paint drawPaint;
	readonly Path drawPath;
	Bitmap? canvasBitmap;
	Canvas? drawCanvas;
	ILine? currentLine;

	/// <summary>
	/// 
	/// </summary>
	public IDrawingView VirtualView { get; }

	/// <summary>
	/// 
	/// </summary>
	public DrawingNativeView(IDrawingView virtualView, Context? context) : base(context)
	{
		VirtualView = virtualView;
		VirtualView.Lines.CollectionChanged += OnLinesCollectionChanged;
		drawPath = new Path();
		drawPaint = new Paint
		{
			AntiAlias = true
		};

		drawPaint.SetStyle(Paint.Style.Stroke);
		drawPaint.StrokeJoin = Paint.Join.Round;
		drawPaint.StrokeCap = Paint.Cap.Round;

		canvasPaint = new Paint
		{
			Dither = true
		};
		drawPaint.Color = VirtualView.DefaultLineColor.ToNative();
		drawPaint.StrokeWidth = VirtualView.DefaultLineWidth;
	}

	void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadLines();

	/// <summary>
	/// 
	/// </summary>
	/// <param name="w"></param>
	/// <param name="h"></param>
	/// <param name="oldw"></param>
	/// <param name="oldh"></param>
	/// <exception cref="NullReferenceException"></exception>
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

	/// <summary>
	/// 
	/// </summary>
	/// <param name="canvas"></param>
	protected override void OnDraw(Canvas? canvas)
	{
		base.OnDraw(canvas);

		if (canvas is not null && canvasBitmap is not null)
		{
			Draw(VirtualView.Lines, canvas);

			canvas.DrawBitmap(canvasBitmap, 0, 0, canvasPaint);
			canvas.DrawPath(drawPath, drawPaint);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="e"></param>
	/// <returns></returns>
	public override bool OnTouchEvent(MotionEvent? e)
	{
		ArgumentNullException.ThrowIfNull(e);
		var touchX = e.GetX();
		var touchY = e.GetY();

		switch (e.Action)
		{
			case MotionEventActions.Down:
				Parent?.RequestDisallowInterceptTouchEvent(true);
				if (!VirtualView.MultiLineMode)
				{
					VirtualView.Lines.Clear();
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
					VirtualView.Lines.Add(currentLine);
					OnDrawingLineCompleted(currentLine);
				}

				if (VirtualView.ClearOnFinish)
				{
					VirtualView.Lines.Clear();
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
		var lines = VirtualView.Lines;
		Draw(lines, drawCanvas, drawPath);

		Invalidate();
	}

	void Draw(IEnumerable<ILine> lines, in Canvas canvas, Path? path = null)
	{
		foreach (var line in lines)
		{
			path ??= new Path();
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

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposed)
		{
			return;
		}

		if (disposing)
		{
			drawCanvas?.Dispose();
			drawPaint.Dispose();
			drawPath.Dispose();
			canvasBitmap?.Dispose();
			canvasPaint.Dispose();
			VirtualView.Lines.CollectionChanged -= OnLinesCollectionChanged;
		}

		disposed = true;

		base.Dispose(disposing);
	}

	Android.Graphics.Color GetBackgroundColor()
	{
		var background = VirtualView.Background?.BackgroundColor ??  Colors.White;
		return background.ToNative();
	}

	/// <summary>
	/// Executes DrawingLineCompleted event and DrawingLineCompletedCommand
	/// </summary>
	/// <param name="lastDrawingLine">Last drawing line</param>
	void OnDrawingLineCompleted(ILine? lastDrawingLine)
	{
		if (VirtualView.DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			VirtualView.DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}