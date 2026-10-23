using Microsoft.Maui.Platform;
using Microsoft.UI.Xaml.Input;
using WBrush = Microsoft.UI.Xaml.Media.Brush;
using WColor = Microsoft.UI.Colors;
using WSolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// DrawingView Native Control
/// </summary>
public partial class MauiDrawingView : PlatformTouchGraphicsView, IDisposable
{
	bool isDisposed;

	/// <inheritdoc />
	~MauiDrawingView() => Dispose(false);

	/// <summary>
	/// Initialize resources
	/// </summary>
	public void Initialize()
	{
		if (OperatingSystem.IsWindowsVersionAtLeast(10, 0, 18362))
		{
			((Microsoft.Maui.Graphics.Win2D.W2DGraphicsView)Content).Drawable = new DrawingViewDrawable(this);
		}
		else
		{
			System.Diagnostics.Trace.WriteLine("DrawingView requires Windows 10.0.18362 or higher.");
		}

		Lines.CollectionChanged += OnLinesCollectionChanged;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	protected virtual void Dispose(bool disposing)
	{
		if (!isDisposed)
		{
			if (disposing)
			{
				currentPath.Dispose();
			}

			isDisposed = true;
		}
	}

	/// <inheritdoc />
	protected override void OnPointerPressed(PointerRoutedEventArgs e)
	{
		base.OnPointerPressed(e);

		// Without the capture, WinUI hands the moves and the release outside the view to other elements, so a line released outside would never
		// end. Android and iOS keep sending a touch to the view where it began.
		CapturePointer(e.Pointer);

		var wPoint = e.GetCurrentPoint(this).Position;
		OnStart(new PointF(wPoint._x, wPoint._y));
	}

	/// <inheritdoc />
	protected override void OnPointerMoved(PointerRoutedEventArgs e)
	{
		base.OnPointerMoved(e);
		var wPoint = e.GetCurrentPoint(this).Position;
		OnMoving(new PointF(wPoint._x, wPoint._y));
	}

	/// <inheritdoc />
	protected override void OnPointerReleased(PointerRoutedEventArgs e)
	{
		base.OnPointerReleased(e);
		OnRelease();

		// Only after OnRelease: ReleasePointerCapture raises PointerCaptureLost at once, which cancels a line that is still being drawn.
		ReleasePointerCapture(e.Pointer);
	}

	/// <inheritdoc />
	protected override void OnPointerCanceled(PointerRoutedEventArgs e)
	{
		base.OnPointerCanceled(e);
		OnCancel();
	}

	/// <inheritdoc />
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs e)
	{
		base.OnPointerCaptureLost(e);
		OnCaptureLost();
	}

	// A release also reaches the view when the press began on another element. It ends no line, and OnFinish would clear the lines when
	// ShouldClearOnFinish is set.
	void OnRelease()
	{
		if (!isDrawing)
		{
			return;
		}

		// Cleared before OnFinish raises DrawingLineCompleted: a handler that removes the view from its window makes WinUI raise PointerCaptureLost
		// during that event, which must not cancel the line that was just completed.
		isDrawing = false;
		OnFinish();
	}

	// Alt+Tab, another window or a system gesture can take the pointer while a line is being drawn. WinUI also raises PointerCaptureLost after
	// each release, and for a pointer that only hovers over the view.
	void OnCaptureLost()
	{
		if (isDrawing)
		{
			OnCancel();
		}
	}

	void Redraw()
	{
		Invalidate();
	}
}