using Android.Graphics;
using Microsoft.Maui.Platform;
using Color = Microsoft.Maui.Graphics.Color;
using Math = System.Math;
using Paint = Android.Graphics.Paint;
using Point = Microsoft.Maui.Graphics.Point;

namespace CommunityToolkit.Maui.Core.Views;

public static partial class DrawingViewService
{
	public static Stream GetImageStream(IList<ILine>? lines,
		Size imageSize,
		Color backgroundColor)
	{
		if (lines is null)
		{
			return Stream.Null;
		}

		var image = GetImageInternal(lines, backgroundColor);
		if (image is null)
		{
			return Stream.Null;
		}

		using var resizedImage = MaxResizeImage(image, (float)imageSize.Width, (float)imageSize.Height);
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

	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static Stream GetImageStream(IList<Microsoft.Maui.Graphics.Point>? points,
		Size imageSize,
		float lineWidth,
		Color strokeColor,
		Color backgroundColor)
	{
		if (points is null || points.Count < 2)
		{
			return Stream.Null;
		}

		var image = GetImageInternal(points, lineWidth, strokeColor, backgroundColor);
		if (image is null)
		{
			return Stream.Null;
		}

		using var resizedImage = MaxResizeImage(image, (float)imageSize.Width, (float)imageSize.Height);
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

	static Bitmap? GetImageInternal(IList<Point> points,
		float lineWidth,
		Color strokeColor,
		Color backgroundColor)
	{
		if (points.Count == 0)
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

		var image = Bitmap.CreateBitmap((int)drawingWidth, (int)drawingHeight, Bitmap.Config.Argb8888!)!;
		using var canvas = new Canvas(image);

		// background
		canvas.DrawColor(backgroundColor.ToNative());

		// strokes
		using var paint = new Paint
		{
			Color = strokeColor.ToNative(),
			StrokeWidth = lineWidth,
			StrokeJoin = Paint.Join.Round,
			StrokeCap = Paint.Cap.Round,
			AntiAlias = true
		};

		paint.SetStyle(Paint.Style.Stroke);

		var pointsCount = points.Count;
		for (var i = 0; i < pointsCount - 1; i++)
		{
			var p1 = points.ElementAt(i);
			var p2 = points.ElementAt(i + 1);
			canvas.DrawLine((float)(p1.X - minPointX), (float)(p1.Y - minPointY), (float)(p2.X - minPointX),
				(float)(p2.Y - minPointY), paint);
		}

		return image;
	}

	static Bitmap? GetImageInternal(IList<ILine> lines,
		Color backgroundColor)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		if (points.Count == 0)
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

		var image = Bitmap.CreateBitmap((int)drawingWidth, (int)drawingHeight, Bitmap.Config.Argb8888!)!;
		using var canvas = new Canvas(image);

		// background
		canvas.DrawColor(backgroundColor.ToNative());

		foreach (var line in lines)
		{
			using var paint = new Paint
			{
				Color = line.LineColor.ToNative(),
				StrokeWidth = line.LineWidth,
				StrokeJoin = Paint.Join.Round,
				StrokeCap = Paint.Cap.Round,
				AntiAlias = true
			};

			paint.SetStyle(Paint.Style.Stroke);

			var pointsCount = line.Points.Count;
			for (var i = 0; i < pointsCount - 1; i++)
			{
				var p1 = line.Points.ElementAt(i);
				var p2 = line.Points.ElementAt(i + 1);
				canvas.DrawLine((float)(p1.X - minPointX), (float)(p1.Y - minPointY), (float)(p2.X - minPointX),
					(float)(p2.Y - minPointY), paint);
			}
		}

		return image;
	}

	static Bitmap MaxResizeImage(Bitmap sourceImage, float maxWidth, float maxHeight)
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