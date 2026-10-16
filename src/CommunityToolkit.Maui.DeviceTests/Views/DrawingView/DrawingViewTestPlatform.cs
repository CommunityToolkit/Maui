using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// What the DrawingView tests need from each platform: creating and hosting a <c>MauiDrawingView</c>, producing the platform's
/// touch input, and turning rendered views and PNG streams into <see cref="DrawingPixelImage"/>s. The platform partials
/// (<c>.android.cs</c>, <c>.macios.cs</c>, <c>.windows.cs</c>) implement the same members:
/// <list type="bullet">
/// <item><c>MauiDrawingView CreateView(double width, double height)</c>: sized in logical units (dp, points, DIPs), not yet initialized.</item>
/// <item><c>Task&lt;T&gt; HostAsync&lt;T&gt;(MauiDrawingView view, IMauiContext mauiContext, Func&lt;Task&lt;T&gt;&gt; body)</c>: adds the view to the runner's window and runs <c>body</c> on the UI thread.</item>
/// <item><c>TouchDown/TouchMove/TouchUp/TouchCancel(MauiDrawingView view, double x, double y)</c>: the platform's real input path; coordinates in logical units.</item>
/// <item><c>Task&lt;DrawingPixelImage&gt; RenderAsync(MauiDrawingView view, IMauiContext mauiContext)</c>: UI thread only.</item>
/// <item><c>Task&lt;DrawingPixelImage&gt; DecodePngAsync(Stream png)</c></item>
/// <item><c>IDrawable? GetDrawable(MauiDrawingView view)</c>: the native view's drawable.</item>
/// <item><c>object? GetParent(MauiDrawingView view)</c>: the native parent view (Android parent, UIKit superview, WinUI parent), or null. WinUI reports
/// the parent only while the view is in a window.</item>
/// <item><c>bool IsChildOf(MauiDrawingView view, object platformParent)</c>: whether the view is a child of the native view <c>platformParent</c>, also while
/// both are out of the window (a WinUI panel's children).</item>
/// </list>
/// Windows cannot construct <c>PointerRoutedEventArgs</c>, so its touch helpers call the shared <c>OnStart/OnMoving/OnFinish/OnCancel</c>
/// methods of the view by reflection; the pointer overrides themselves are reported as skipped there.
/// </summary>
static partial class DrawingViewTestPlatform
{
	public const double DefaultWidth = 200;

	public const double DefaultHeight = 100;

	/// <summary>True on iOS and Mac Catalyst, which share MauiDrawingView.macios.cs and DrawingViewService.macios.cs.</summary>
	internal static bool IsApple => OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst();

	/// <summary>
	/// Like <c>AssertionExtensions.WaitForGC</c>, but forces a collection every 100 ms for up to 15 s instead of 40 back-to-back ones.
	/// A view that was hosted in a window (and drawn) is only released on Android after the Java GC bridge and a later frame ran,
	/// which takes wall-clock time: the same views are collected after about one second here, but not within 40 immediate collections.
	/// </summary>
	internal static async Task WaitUntilCollectedAsync(params WeakReference[] references)
	{
		Assert.NotEmpty(references);

		try
		{
			await AssertionExtensions.AssertEventuallyAsync(async () =>
			{
				await AssertionExtensions.Collect();
				return references.All(reference => !reference.IsAlive);
			}, timeout: 15000, interval: 100);
		}
		catch (Xunit.Sdk.XunitException)
		{
			var survivors = string.Join(", ", references.Select(reference => reference.Target).Where(target => target is not null));
			throw new Xunit.Sdk.XunitException($"Expected all references to be collected within 15 s of forced collections, but these are still alive: {survivors}");
		}
	}

	/// <summary>
	/// Waits until the view was drawn at least once (<paramref name="drawCount"/> &gt; 0) and then stopped redrawing (the count did not change
	/// for 150 ms). Returns the count at that point, so a later increase proves that something requested a new draw.
	/// </summary>
	internal static async Task<int> WaitForDrawsToSettleAsync(Func<int> drawCount)
	{
		await AssertionExtensions.AssertEventually(() => drawCount() > 0, timeout: 5000, message: "The view was never drawn.");

		var last = -1;
		await AssertionExtensions.AssertEventually(() =>
		{
			var current = drawCount();
			var settled = current == last;
			last = current;
			return settled;
		}, timeout: 5000, interval: 150, message: "The view kept redrawing.");

		return drawCount();
	}

	/// <summary>
	/// Gradient paints that MAUI accepts but that are degenerate for a platform gradient API: "linear-1-stop" and "radial-1-stop" (a single red stop)
	/// and "radial-radius-0" (red to blue with a radius of 0).
	/// </summary>
	internal static Paint CreateDegenerateGradient(string kind) => kind switch
	{
		"linear-1-stop" => new LinearGradientPaint([new PaintGradientStop(0, Colors.Red)], new Point(0, 0.5), new Point(1, 0.5)),
		"radial-1-stop" => new RadialGradientPaint(new Point(0.5, 0.5), 0.5) { GradientStops = [new PaintGradientStop(0, Colors.Red)] },
		"radial-radius-0" => new RadialGradientPaint(new Point(0.5, 0.5), 0) { GradientStops = [new PaintGradientStop(0, Colors.Red), new PaintGradientStop(1, Colors.Blue)] },
		"linear-no-stops" => WithoutStops(new LinearGradientPaint(new Point(0, 0), new Point(1, 1))),
		"radial-no-stops" => WithoutStops(new RadialGradientPaint(new Point(0.5, 0.5), 0.5)),
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	// The GradientStops setter replaces an empty array with two white stops: only SetGradientStops leaves a gradient without stops.
	static GradientPaint WithoutStops(GradientPaint paint)
	{
		paint.SetGradientStops([], []);
		Assert.Empty(paint.GradientStops);
		return paint;
	}

	/// <summary>A complete gesture: down at the first point, a move to each following point, up at the last point.</summary>
	internal static void Stroke(MauiDrawingView view, params (double X, double Y)[] points)
	{
		TouchDown(view, points[0].X, points[0].Y);
		foreach (var point in points.Skip(1))
		{
			TouchMove(view, point.X, point.Y);
		}

		TouchUp(view, points[^1].X, points[^1].Y);
	}
}