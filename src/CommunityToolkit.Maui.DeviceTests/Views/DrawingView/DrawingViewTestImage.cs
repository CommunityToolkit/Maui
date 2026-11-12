using Xunit.Sdk;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>One non-premultiplied pixel, 0-255 per channel.</summary>
readonly record struct DrawingRgba(int R, int G, int B, int A)
{
	public const int DefaultTolerance = 8;

	public static DrawingRgba FromColor(Color color) => new(To255(color.Red), To255(color.Green), To255(color.Blue), To255(color.Alpha));

	public bool IsNear(DrawingRgba other, int tolerance = DefaultTolerance) =>
		Math.Abs(R - other.R) <= tolerance
		&& Math.Abs(G - other.G) <= tolerance
		&& Math.Abs(B - other.B) <= tolerance
		&& Math.Abs(A - other.A) <= tolerance;

	public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

	static int To255(float value) => (int)Math.Round(value * 255f);
}

/// <summary>
/// <see cref="Scale"/> is the number of pixels per logical unit (dp on Android, points on Apple platforms, DIPs on Windows),
/// so assertions use the same coordinates as the touches.
/// </summary>
sealed class DrawingPixelImage(int width, int height, DrawingRgba[] pixels, double scale)
{
	public int Width { get; } = width;

	public int Height { get; } = height;

	public double Scale { get; } = scale;

	public DrawingRgba this[int x, int y]
	{
		get
		{
			if (x < 0 || y < 0 || x >= Width || y >= Height)
			{
				throw new ArgumentOutOfRangeException(nameof(x), $"Pixel ({x},{y}) is outside the {Width}x{Height} image.");
			}

			return pixels[(y * Width) + x];
		}
	}

	public DrawingRgba AtUnits(double x, double y) => this[ToPixel(x, Width), ToPixel(y, Height)];

	public int CountNear(Color color, int tolerance = DrawingRgba.DefaultTolerance)
	{
		var expected = DrawingRgba.FromColor(color);
		var count = 0;
		foreach (var pixel in pixels)
		{
			if (pixel.IsNear(expected, tolerance))
			{
				count++;
			}
		}

		return count;
	}

	int ToPixel(double units, int limit) => Math.Clamp((int)Math.Round(units * Scale), 0, limit - 1);
}

static class DrawingPixelAssert
{
	public static void ColorAtUnits(DrawingPixelImage image, double x, double y, Color expected, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		var actual = image.AtUnits(x, y);
		var wanted = DrawingRgba.FromColor(expected);
		if (!actual.IsNear(wanted, tolerance))
		{
			throw new XunitException($"Expected {wanted} at ({x},{y}) units but found {actual}. {because} [image {image.Width}x{image.Height}px, {image.Scale} px/unit]");
		}
	}

	public static void NotColorAtUnits(DrawingPixelImage image, double x, double y, Color unexpected, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		var actual = image.AtUnits(x, y);
		var forbidden = DrawingRgba.FromColor(unexpected);
		if (actual.IsNear(forbidden, tolerance))
		{
			throw new XunitException($"Did not expect {forbidden} at ({x},{y}) units but found {actual}. {because} [image {image.Width}x{image.Height}px, {image.Scale} px/unit]");
		}
	}

	public static void SegmentHasColor(DrawingPixelImage image, (double X, double Y) from, (double X, double Y) to, Color expected, int samples = 5, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		for (var i = 1; i <= samples; i++)
		{
			var t = i / (double)(samples + 1);
			ColorAtUnits(image, from.X + ((to.X - from.X) * t), from.Y + ((to.Y - from.Y) * t), expected, tolerance, because);
		}
	}

	public static void ContainsColor(DrawingPixelImage image, Color expected, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		if (image.CountNear(expected, tolerance) is 0)
		{
			throw new XunitException($"Expected {DrawingRgba.FromColor(expected)} somewhere in the image but found none. {because} [image {image.Width}x{image.Height}px]");
		}
	}

	public static void DoesNotContainColor(DrawingPixelImage image, Color unexpected, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		var count = image.CountNear(unexpected, tolerance);
		if (count is not 0)
		{
			throw new XunitException($"Did not expect {DrawingRgba.FromColor(unexpected)} but found {count} such pixels. {because} [image {image.Width}x{image.Height}px]");
		}
	}

	public static void SameImage(DrawingPixelImage expected, DrawingPixelImage actual, int tolerance = DrawingRgba.DefaultTolerance, string? because = null)
	{
		if (expected.Width != actual.Width || expected.Height != actual.Height)
		{
			throw new XunitException($"Expected a {expected.Width}x{expected.Height} image but it is {actual.Width}x{actual.Height}. {because}");
		}

		for (var y = 0; y < expected.Height; y++)
		{
			for (var x = 0; x < expected.Width; x++)
			{
				if (!expected[x, y].IsNear(actual[x, y], tolerance))
				{
					throw new XunitException($"The images differ at pixel ({x},{y}): expected {expected[x, y]} but found {actual[x, y]}. {because} [image {expected.Width}x{expected.Height}px]");
				}
			}
		}
	}

	public static void SizeIs(DrawingPixelImage image, int width, int height, int tolerance = 0)
	{
		if (Math.Abs(image.Width - width) > tolerance || Math.Abs(image.Height - height) > tolerance)
		{
			throw new XunitException($"Expected a {width}x{height} image but it is {image.Width}x{image.Height}.");
		}
	}
}