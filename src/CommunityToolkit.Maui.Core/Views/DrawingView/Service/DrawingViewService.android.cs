using Android.Graphics;
using CommunityToolkit.Maui.Core.Extensions;
using Microsoft.Maui.Platform;
using Color = Microsoft.Maui.Graphics.Color;
using Math = System.Math;
using Paint = Android.Graphics.Paint;
using PointF = Microsoft.Maui.Graphics.PointF;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Drawing view service
/// </summary>
public static partial class DrawingViewService
{
	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(in IList<IDrawingLine> lines,
												   in Size imageSize,
												   in Color? backgroundColor)
	{

		var image = GetBitmapForLines(lines, backgroundColor);
		return ValueTask.FromResult(GetBitmapStream(image, imageSize));
	}

	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(in IList<PointF> points,
										in Size imageSize,
										in float lineWidth,
										in Color strokeColor,
										in Color? backgroundColor)
	{
		var image = GetBitmapForPoints(points, lineWidth, strokeColor, backgroundColor);
		return ValueTask.FromResult(GetBitmapStream(image, imageSize));
	}

	static Stream GetBitmapStream(Bitmap? image, Size imageSize)
	{
		if (image is null)
		{
			return Stream.Null;
		}

		using var resizedImage = GetMaximumBitmap(image, (float)imageSize.Width, (float)imageSize.Height);
		var stream = new MemoryStream();
		var compressResult = resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

		resizedImage.Recycle();

		if (!compressResult)
		{
			return Stream.Null;
		}

		stream.Position = 0;
		return stream;
	}

	static Bitmap? GetBitmapForPoints(ICollection<PointF> points,
		float lineWidth,
		Color strokeColor,
		Color? backgroundColor)
	{
		var (image, offset) = GetBitmap(points);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);
		canvas.DrawColor(backgroundColor.ToPlatform(DrawingViewDefaults.BackgroundColor));
		DrawStrokes(canvas, points, lineWidth, strokeColor, offset);
		return image;
	}

	static Bitmap? GetBitmapForLines(IList<IDrawingLine> lines, Color? backgroundColor)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		var (image, offset) = GetBitmap(points);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);
		canvas.DrawColor(backgroundColor.ToPlatform(DrawingViewDefaults.BackgroundColor));
		foreach (var line in lines)
		{
			DrawStrokes(canvas, line.Points, line.LineWidth, line.LineColor, offset);
		}

		return image;
	}

	static (Bitmap?, SizeF offset) GetBitmap(ICollection<PointF> points)
	{
		if (points.Count is 0)
		{
			return (null, SizeF.Zero);
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		const int minSize = 1;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return (null, SizeF.Zero);
		}

		if (Bitmap.Config.Argb8888 is null)
		{
			return (null, SizeF.Zero);
		}

		var image = Bitmap.CreateBitmap((int)drawingWidth, (int)drawingHeight, Bitmap.Config.Argb8888);
		if (image is null)
		{
			return (null, SizeF.Zero);
		}

		return (image, new SizeF(minPointX, minPointY));
	}

	static void DrawStrokes(Canvas canvas, ICollection<PointF> points, float lineWidth, Color strokeColor, SizeF offset)
	{
		using var paint = new Paint
		{
			StrokeWidth = lineWidth,
			StrokeJoin = Paint.Join.Round,
			StrokeCap = Paint.Cap.Round,
			AntiAlias = true
		};

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			paint.Color = strokeColor.ToPlatform();
		}

		paint.SetStyle(Paint.Style.Stroke);

		var pointsCount = points.Count;
		for (var i = 0; i < pointsCount - 1; i++)
		{
			var p1 = points.ElementAt(i);
			var p2 = points.ElementAt(i + 1);

			canvas.DrawLine(p1.X - offset.Width, p1.Y - offset.Height, p2.X - offset.Width, p2.Y - offset.Height, paint);
		}
	}

	static Bitmap GetMaximumBitmap(in Bitmap sourceImage, in float maxWidth, in float maxHeight)
	{
		var sourceSize = new Size(sourceImage.Width, sourceImage.Height);
		var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);
		if (maxResizeFactor > 1)
		{
			return sourceImage;
		}

		var width = maxResizeFactor * sourceSize.Width;
		var height = maxResizeFactor * sourceSize.Height;
		return Bitmap.CreateScaledBitmap(sourceImage, (int)width, (int)height, false)!;
	}
}