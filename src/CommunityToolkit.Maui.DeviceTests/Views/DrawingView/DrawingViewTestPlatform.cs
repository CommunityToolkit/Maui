using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

static partial class DrawingViewTestPlatform
{
	public const double DefaultWidth = 200;

	public const double DefaultHeight = 100;

	internal static bool IsApple => OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst();

	/// <summary>
	/// A view that was hosted in a window (and drawn) is only released on Android after the Java GC bridge and a later frame ran, which takes
	/// wall-clock time: the same views are collected after about one second here, but not within the 40 back-to-back collections of <c>AssertionExtensions.WaitForGC</c>.
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

	/// <summary>The returned count lets a later increase prove that something requested a new draw.</summary>
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