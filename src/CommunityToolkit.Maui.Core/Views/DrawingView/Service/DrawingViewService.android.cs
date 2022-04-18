using Android.Graphics;
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
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		using var resizedImage = GetMaximumBitmap(image, (float)imageSize.Width, (float)imageSize.Height);
		var stream = new MemoryStream();
		var compressResult = resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

		resizedImage.Recycle();

		if (!compressResult)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		stream.Position = 0;
		return ValueTask.FromResult<Stream>(stream);
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
		if (points.Count < 2)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		var image = GetBitmapForPoints(points, lineWidth, strokeColor, backgroundColor);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		using var resizedImage = GetMaximumBitmap(image, (float)imageSize.Width, (float)imageSize.Height);
		var stream = new MemoryStream();
		var compressResult = resizedImage.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

		resizedImage.Recycle();

		if (!compressResult)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		stream.Position = 0;
		return ValueTask.FromResult<Stream>(stream);
	}

	static Bitmap? GetBitmapForPoints(ICollection<PointF> points,
		float lineWidth,
		Color strokeColor,
		Color? backgroundColor)
	{
		if (points.Count is 0)
		{
			return null;
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		const int minSize = 1;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		if (Bitmap.Config.Argb8888 is null)
		{
			return null;
		}

		var image = Bitmap.CreateBitmap((int)drawingWidth, (int)drawingHeight, Bitmap.Config.Argb8888);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);

		// background
		canvas.DrawColor(backgroundColor.ToPlatform(Defaults.BackgroundColor));

		// strokes
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

			canvas.DrawLine(p1.X - minPointX, p1.Y - minPointY, p2.X - minPointX, p2.Y - minPointY, paint);
		}

		return image;
	}

	static Bitmap? GetBitmapForLines(IList<IDrawingLine> lines, Color? backgroundColor)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		if (points.Count is 0)
		{
			return null;
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		const int minSize = 1;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		if (Bitmap.Config.Argb8888 is null)
		{
			return null;
		}

		var image = Bitmap.CreateBitmap((int)drawingWidth, (int)drawingHeight, Bitmap.Config.Argb8888);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);

		// background
		canvas.DrawColor(backgroundColor.ToPlatform(Defaults.BackgroundColor));

		foreach (var line in lines)
		{
			using var paint = new Paint
			{
				StrokeWidth = line.LineWidth,
				StrokeJoin = Paint.Join.Round,
				StrokeCap = Paint.Cap.Round,
				AntiAlias = true
			};

			if (OperatingSystem.IsAndroidVersionAtLeast(29))
			{
				paint.Color = line.LineColor.ToPlatform();
			}

			paint.SetStyle(Paint.Style.Stroke);

			var pointsCount = line.Points.Count;
			for (var i = 0; i < pointsCount - 1; i++)
			{
				var p1 = line.Points.ElementAt(i);
				var p2 = line.Points.ElementAt(i + 1);
				canvas.DrawLine(p1.X - minPointX, p1.Y - minPointY, p2.X - minPointX, p2.Y - minPointY, paint);
			}
		}

		return image;
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