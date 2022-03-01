using Android.Content;
using Android.Graphics;
using Android.Views;
using View = Android.Views.View;
using Point = Microsoft.Maui.Graphics.Point;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.UI.Views;

/// <summary>
/// Android renderer for <see cref="CommunityToolkit.Maui.UI.Views.DrawingViewHandler"/>
/// </summary>
/// 
public class DrawingNativeView : View
{

	readonly Paint canvasPaint;
	readonly Paint drawPaint;
	readonly Path drawPath;
	Bitmap? canvasBitmap;
	Canvas? drawCanvas;
	Line? currentLine;

	public DrawingView VirtualView { get; }

	public DrawingNativeView(DrawingView virtualView, Context? context) : base(context)
	{
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
		VirtualView = virtualView;
		drawPaint.Color = VirtualView.DefaultLineColor.ToAndroid();
		drawPaint.StrokeWidth = VirtualView.DefaultLineWidth;
	}


	protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
	{
		const int minW = 1;
		const int minH = 1;
		w = w < minW ? minW : w;
		h = h < minH ? minH : h;
		base.OnSizeChanged(w, h, oldw, oldh);

		canvasBitmap = Bitmap.CreateBitmap(w, h, Bitmap.Config.Argb8888!)!;
		drawCanvas = new Canvas(canvasBitmap);
		LoadLines();
	}


	protected override void OnDraw(Canvas? canvas)
	{
		base.OnDraw(canvas);

		foreach (var line in VirtualView.Lines)
		{
			var path = new Path();
			path.MoveTo((float)line.Points[0].X, (float)line.Points[0].Y);
			foreach (var (x, y) in line.Points)
				path.LineTo((float)x, (float)y);

			canvas?.DrawPath(path, drawPaint);
		}

		canvas?.DrawBitmap(canvasBitmap!, 0, 0, canvasPaint);
		canvas?.DrawPath(drawPath, drawPaint);
	}

	public override bool OnTouchEvent(MotionEvent? e)
	{
		if (e is null)
		{
			return false;
		}

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
						new Point(touchX, touchY)
					}
				};

				drawCanvas!.DrawColor(VirtualView.BackgroundColor.ToAndroid(), PorterDuff.Mode.Clear!);
				drawPath.MoveTo(touchX, touchY);
				break;
			case MotionEventActions.Move:
				if (touchX > 0 && touchY > 0)
					drawPath.LineTo(touchX, touchY);

				currentLine!.Points.Add(new Point(touchX, touchY));
				break;
			case MotionEventActions.Up:
				Parent?.RequestDisallowInterceptTouchEvent(false);
				drawCanvas!.DrawPath(drawPath, drawPaint);
				drawPath.Reset();
				if (currentLine != null)
				{
					VirtualView.Lines.Add(currentLine);
					VirtualView.OnDrawingLineCompleted(currentLine);
				}

				if (VirtualView.ClearOnFinish)
					VirtualView.Lines.Clear();

				break;
			default:
				return false;
		}

		Invalidate();

		return true;
	}

	public override bool DispatchTouchEvent(MotionEvent? e)
	{            
		if (!Enabled || VirtualView?.IsEnabled == false)
			return true;

		return base.DispatchTouchEvent(e);
	}

	void LoadLines()
	{
		drawCanvas?.DrawColor(VirtualView.BackgroundColor.ToAndroid(), PorterDuff.Mode.Clear!);
		drawPath.Reset();
		var lines = VirtualView.Lines;
		if (lines.Count > 0)
		{
			foreach (var line in lines)
			{
				drawPath.MoveTo((float)line.Points[0].X, (float)line.Points[0].Y);
				foreach (var (x, y) in line.Points)
					drawPath.LineTo((float)x, (float)y);

				drawCanvas?.DrawPath(drawPath, drawPaint);
				drawPath.Reset();
			}
		}

		Invalidate();
	}


	public void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => LoadLines();
}


public partial class DrawingViewHandler : ViewHandler<DrawingView, DrawingNativeView>
{
	public static PropertyMapper<DrawingView, DrawingViewHandler> CustomEntryMapper = new PropertyMapper<DrawingView, DrawingViewHandler>(ViewHandler.ViewMapper)
	{
		[nameof(DrawingView.LinesProperty)] = MapText,
		[nameof(DrawingView.BackgroundColorProperty)] = MapBackgroundColor
	};

	private static void MapBackgroundColor(DrawingViewHandler arg1, DrawingView arg2)
	{
		arg1.NativeView.SetBackgroundColor(arg2.BackgroundColor.ToAndroid());
	}

	private static void MapText(DrawingViewHandler arg1, DrawingView arg2)
	{
		arg1.NativeView.OnLinesCollectionChanged(arg2,new System.Collections.Specialized.NotifyCollectionChangedEventArgs(System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
	}

	public DrawingViewHandler() : base(CustomEntryMapper)
	{

	}

	public DrawingViewHandler(IPropertyMapper mapper, CommandMapper? commandMapper = null) : base(mapper, commandMapper)
	{
	}

	protected override DrawingNativeView CreateNativeView()
	{
		return new DrawingNativeView(VirtualView, Context);
	}

	protected override void ConnectHandler(DrawingNativeView nativeView)
	{      
		nativeView.SetBackgroundColor(VirtualView.BackgroundColor.ToAndroid());            
		VirtualView.Lines.CollectionChanged += nativeView.OnLinesCollectionChanged;
		base.ConnectHandler(nativeView);
	}

	protected override void DisconnectHandler(DrawingNativeView nativeView)
	{
		//drawCanvas!.Dispose();
		//drawPaint.Dispose();
		//drawPath.Dispose();
		//canvasBitmap!.Dispose();
		//canvasPaint.Dispose();
		VirtualView.Lines.CollectionChanged -= nativeView.OnLinesCollectionChanged;
		base.DisconnectHandler(nativeView);
	}
}