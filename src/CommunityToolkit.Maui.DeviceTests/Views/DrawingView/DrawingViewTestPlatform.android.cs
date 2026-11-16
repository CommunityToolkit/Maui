using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;
using ABitmap = Android.Graphics.Bitmap;
using ABitmapFactory = Android.Graphics.BitmapFactory;
using ACanvas = Android.Graphics.Canvas;

namespace CommunityToolkit.Maui.DeviceTests;

static partial class DrawingViewTestPlatform
{
	internal sealed class RecordingFrameLayout(Context context) : FrameLayout(context)
	{
		public List<bool> DisallowInterceptRequests { get; } = [];

		public override void RequestDisallowInterceptTouchEvent(bool disallowIntercept)
		{
			DisallowInterceptRequests.Add(disallowIntercept);
			base.RequestDisallowInterceptTouchEvent(disallowIntercept);
		}
	}

	internal static MauiDrawingView CreateView(double width, double height)
	{
		var view = new MauiDrawingView(MauiProgramDefaults.DefaultContext);
		var density = GetDrawDensity(view);
		view.LayoutParameters = new FrameLayout.LayoutParams((int)Math.Round(width * density), (int)Math.Round(height * density));
		return view;
	}

	internal static Task<T> HostAsync<T>(MauiDrawingView view, IMauiContext mauiContext, Func<Task<T>> body) =>
		TestDispatcher.Current.DispatchAsync(async () =>
		{
			var host = new RecordingFrameLayout(view.Context!);
			host.AddView(view);
			return await host.AttachAndRun(body);
		});

	internal static IDrawable? GetDrawable(MauiDrawingView view) => view.Drawable;

	internal static object? GetParent(MauiDrawingView view) => view.Parent;

	internal static bool IsChildOf(MauiDrawingView view, object platformParent) => ReferenceEquals(view.Parent, platformParent);

	/// <summary>The density the platform view draws with (canvas.DisplayScale).</summary>
	internal static double GetDrawDensity(MauiDrawingView view) => view.Resources!.DisplayMetrics!.Density;

	/// <summary>The density the toolkit divides touch coordinates by.</summary>
	internal static double TouchDensity => DeviceDisplay.MainDisplayInfo.Density;

	internal static bool TouchDown(MauiDrawingView view, double x, double y) => Send(view, MotionEventActions.Down, x * TouchDensity, y * TouchDensity);

	internal static bool TouchMove(MauiDrawingView view, double x, double y) => Send(view, MotionEventActions.Move, x * TouchDensity, y * TouchDensity);

	internal static bool TouchUp(MauiDrawingView view, double x, double y) => Send(view, MotionEventActions.Up, x * TouchDensity, y * TouchDensity);

	internal static bool TouchCancel(MauiDrawingView view, double x, double y) => Send(view, MotionEventActions.Cancel, x * TouchDensity, y * TouchDensity);

	/// <summary>Takes raw pixel coordinates, so tests can hit exact boundary values.</summary>
	internal static bool Send(MauiDrawingView view, MotionEventActions action, double xPixels, double yPixels)
	{
		var now = SystemClock.UptimeMillis();
		var motionEvent = MotionEvent.Obtain(now, now, action, (float)xPixels, (float)yPixels, 0) ?? throw new InvalidOperationException("MotionEvent.Obtain returned null.");
		try
		{
			return view.OnTouchEvent(motionEvent);
		}
		finally
		{
			motionEvent.Recycle();
		}
	}

	// UI thread only.
	internal static async Task<DrawingPixelImage> RenderAsync(MauiDrawingView view, IMauiContext mauiContext)
	{
		await AssertionExtensions.AssertEventually(() => view.Width > 0 && view.Height > 0, timeout: 3000, message: "The drawing view was not laid out.");

		using var bitmap = ABitmap.CreateBitmap(view.Width, view.Height, ABitmap.Config.Argb8888!)!;
		using (var canvas = new ACanvas(bitmap))
		{
			view.Draw(canvas);
		}

		return ToPixelImage(bitmap, GetDrawDensity(view));
	}

	internal static Task<DrawingPixelImage> DecodePngAsync(Stream png)
	{
		using var bitmap = ABitmapFactory.DecodeStream(png) ?? throw new InvalidOperationException("The PNG stream could not be decoded.");
		return Task.FromResult(ToPixelImage(bitmap, 1));
	}

	internal static DrawingPixelImage ToPixelImage(ABitmap bitmap, double scale)
	{
		var width = bitmap.Width;
		var height = bitmap.Height;
		var argb = new int[width * height];
		bitmap.GetPixels(argb, 0, width, 0, 0, width, height);

		var pixels = new DrawingRgba[argb.Length];
		for (var i = 0; i < argb.Length; i++)
		{
			var value = unchecked((uint)argb[i]);
			pixels[i] = new DrawingRgba((int)((value >> 16) & 0xFF), (int)((value >> 8) & 0xFF), (int)(value & 0xFF), (int)(value >> 24));
		}

		return new DrawingPixelImage(width, height, pixels, scale);
	}
}