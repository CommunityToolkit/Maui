using System.Reflection;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Graphics.Win2D;

namespace CommunityToolkit.Maui.DeviceTests;

static partial class DrawingViewTestPlatform
{
	internal static MauiDrawingView CreateView(double width, double height) => new() { Width = width, Height = height };

	internal static Task<T> HostAsync<T>(MauiDrawingView view, IMauiContext mauiContext, Func<Task<T>> body) =>
		TestDispatcher.Current.DispatchAsync(async () => await view.AttachAndRun(async _ => await body(), mauiContext));

	internal static IDrawable? GetDrawable(MauiDrawingView view) => ((W2DGraphicsView)view.Content).Drawable;

	// WinUI reports the parent only while the view is in a window (the live tree): out of it, Parent is null although a panel still holds the view
	internal static object? GetParent(MauiDrawingView view) => view.Parent;

	internal static bool IsChildOf(MauiDrawingView view, object platformParent) => platformParent is Microsoft.UI.Xaml.Controls.Panel panel && panel.Children.Contains(view);

	// WinUI cannot construct PointerRoutedEventArgs, so the shared gesture methods are called directly.
	internal static void TouchDown(MauiDrawingView view, double x, double y) => Invoke(view, "OnStart", new PointF((float)x, (float)y));

	internal static void TouchMove(MauiDrawingView view, double x, double y) => Invoke(view, "OnMoving", new PointF((float)x, (float)y));

	internal static void TouchUp(MauiDrawingView view, double x, double y) => Invoke(view, "OnFinish");

	internal static void TouchCancel(MauiDrawingView view, double x, double y) => Invoke(view, "OnCancel");

	internal static void Invoke(MauiDrawingView view, string methodName, params object[] arguments)
	{
		var method = typeof(MauiDrawingView).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
			?? throw new InvalidOperationException($"{nameof(MauiDrawingView)}.{methodName} was not found.");

		try
		{
			method.Invoke(view, arguments);
		}
		catch (TargetInvocationException ex) when (ex.InnerException is not null)
		{
			ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
		}
	}

	/// <summary>
	/// Draws the view's native drawable into an offscreen Win2D render target of the view's size. Capturing the Win2D control through
	/// the screen is not available on every CI machine (see AssertionExtensions.ToBitmap), so the pixels come from the drawable that
	/// the view's W2DGraphicsView would draw.
	/// </summary>
	internal static Task<DrawingPixelImage> RenderAsync(MauiDrawingView view, IMauiContext mauiContext)
	{
		var drawable = GetDrawable(view) ?? throw new InvalidOperationException("The drawing view has no drawable: Initialize() was not called.");
		var width = (float)Math.Max(view.ActualWidth > 0 ? view.ActualWidth : view.Width, 1);
		var height = (float)Math.Max(view.ActualHeight > 0 ? view.ActualHeight : view.Height, 1);

		using var target = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96);
		using (var session = target.CreateDrawingSession())
		{
			session.Clear(Microsoft.UI.Colors.Transparent);
			drawable.Draw(new W2DCanvas { Session = session }, new RectF(0, 0, width, height));
		}

		return Task.FromResult(ToPixelImage(target, 1));
	}

	internal static async Task<DrawingPixelImage> DecodePngAsync(Stream png)
	{
		// Not disposed: for a stream over a WinRT stream, such as the one DrawingViewService returns on Windows, AsRandomAccessStream returns that
		// WinRT stream itself, and closing it made the caller's own disposal of the stream throw ObjectDisposedException (its flush).
		var randomAccessStream = png.AsRandomAccessStream();
		var bitmap = await CanvasBitmap.LoadAsync(CanvasDevice.GetSharedDevice(), randomAccessStream);
		return ToPixelImage(bitmap, 1);
	}

	internal static DrawingPixelImage ToPixelImage(CanvasBitmap bitmap, double scale)
	{
		var colors = bitmap.GetPixelColors();
		var pixels = new DrawingRgba[colors.Length];
		for (var i = 0; i < colors.Length; i++)
		{
			pixels[i] = new DrawingRgba(colors[i].R, colors[i].G, colors[i].B, colors[i].A);
		}

		return new DrawingPixelImage((int)bitmap.SizeInPixels.Width, (int)bitmap.SizeInPixels.Height, pixels, scale);
	}
}