using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Maui.Core.Extensions;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Views;
using Microsoft.Maui.Platform;
using APaint = Android.Graphics.Paint;
using APath = Android.Graphics.Path;
using AView = Android.Views.View;
using AColor = Android.Graphics.Color;
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
	Bitmap? canvasBitmap;
	Canvas? drawCanvas;
	DrawingNativeLine? currentLine;
	
	/// <summary>
	/// Event raised when drawing line completed 
	/// </summary>
	public event EventHandler<DrawingNativeLineCompletedEventArgs>? DrawingLineCompleted;

	/// <summary>
	/// Line color
	/// </summary>
	public AColor LineColor { get; set; }

	/// <summary>
	/// Line width
	/// </summary>
	public float LineWidth { get; set; }

	/// <summary>
	/// Command executed when drawing line completed
	/// </summary>
	public ICommand? DrawingLineCompletedCommand { get; set; }

	/// <summary>
	/// Drawing Lines
	/// </summary>
	public ObservableCollection<DrawingNativeLine> Lines { get; }

	/// <summary>
	/// Enable or disable multiline mode
	/// </summary>
	public bool MultiLineMode { get; set; }

	/// <summary>
	/// Clear drawing on finish
	/// </summary>
	public bool ClearOnFinish { get; set; }

	/// <summary>
	/// Initialize a new instance of <see cref="DrawingNativeView" />.
	/// </summary>
	public DrawingNativeView(Context? context) : base(context)
	{
		Lines = new ObservableCollection<DrawingNativeLine>();
		LineColor = Colors.Black.ToNative();
		LineWidth = 5;
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
	}

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		Lines.CollectionChanged += OnLinesCollectionChanged;
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
				if (!MultiLineMode)
				{
					Lines.Clear();
				}

				currentLine = new DrawingNativeLine()
				{
					Points = new ObservableCollection<Point>
						{
							new (touchX, touchY)
						}
				};

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
		var lines = Lines;
		Draw(lines, drawCanvas, drawPath);

		Invalidate();
	}

	void Draw(IEnumerable<DrawingNativeLine> lines, in Canvas canvas, APath? path = null)
	{
		drawPaint.Color = LineColor;
		drawPaint.StrokeWidth = LineWidth;
		foreach (var line in lines)
		{
			path ??= new APath();
			var points = NormalizePoints(line.EnableSmoothedPath
					? line.Points.SmoothedPathWithGranularity(line.Granularity)
					: line.Points);
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
		Lines.CollectionChanged -= OnLinesCollectionChanged;
	}

	AColor GetBackgroundColor()
	{
		if (Background is ColorDrawable colorDrawable)
		{
			return colorDrawable.Color;
		}

		return AColor.White;
	}

	void OnDrawingLineCompleted(DrawingNativeLine? lastDrawingLine)
	{
		DrawingLineCompleted?.Invoke(this, new DrawingNativeLineCompletedEventArgs(lastDrawingLine));
		if (DrawingLineCompletedCommand?.CanExecute(lastDrawingLine) ?? false)
		{
			DrawingLineCompletedCommand.Execute(lastDrawingLine);
		}
	}
}