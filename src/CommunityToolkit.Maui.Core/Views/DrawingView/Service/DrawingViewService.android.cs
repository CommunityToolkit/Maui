using Android.Graphics;
using Microsoft.Maui.Platform;
using APaint = Android.Graphics.Paint;
using Color = Microsoft.Maui.Graphics.Color;
using Math = System.Math;
using Paint = Microsoft.Maui.Graphics.Paint;
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
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImageLineOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		var image = GetBitmapForLines(options.Lines, options.Background, options.CanvasSize);

		return ValueTask.FromResult(GetBitmapStream(image, options.DesiredSize));
	}

	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImagePointOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		var image = GetBitmapForPoints(options.Points, options.LineWidth, options.StrokeColor, options.Background, options.CanvasSize);

		return ValueTask.FromResult(GetBitmapStream(image, options.DesiredSize));
	}

	static Stream GetBitmapStream(Bitmap? image, Size imageSize)
	{
		if (image is null)
		{
			return Stream.Null;
		}

		using var resizedImage = GetMaximumBitmap(image, (float)imageSize.Width, (float)imageSize.Height);
		var stream = new MemoryStream();
		var compressResult = resizedImage.Compress(Bitmap.CompressFormat.Png ?? throw new InvalidOperationException($"{nameof(Bitmap)}.{nameof(Bitmap.CompressFormat)}.{nameof(Bitmap.CompressFormat.Png)} cannot be null"),
													100, stream);

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
		Paint? background,
		Size? canvasSize)
	{
		var (image, offset) = GetBitmap(points, lineWidth, canvasSize);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);
		DrawBackground(canvas, background);
		DrawStrokes(canvas, points, lineWidth, strokeColor, offset);
		return image;
	}

	static Bitmap? GetBitmapForLines(IList<IDrawingLine> lines, Paint? background, Size? canvasSize)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		if (points.Count is 0)
		{
			// Max() below needs a line, and every platform returns Stream.Null when there is nothing to draw
			return null;
		}

		var maxLineWidth = lines.Select(x => x.LineWidth).Max();
		var (image, offset) = GetBitmap(points, maxLineWidth, canvasSize);
		if (image is null)
		{
			return null;
		}

		using var canvas = new Canvas(image);
		DrawBackground(canvas, background);
		foreach (var line in lines)
		{
			DrawStrokes(canvas, line.Points, line.LineWidth, line.LineColor, offset);
		}

		return image;
	}

	static (Bitmap?, SizeF offset) GetBitmap(ICollection<PointF> points, float maxLineWidth, Size? canvasSize)
	{
		if (points.Count is 0)
		{
			return (null, SizeF.Zero);
		}

		var minPointX = points.Min(p => p.X) - maxLineWidth;
		var minPointY = points.Min(p => p.Y) - maxLineWidth;
		var drawingWidth = canvasSize?.Width ?? points.Max(p => p.X) - minPointX + maxLineWidth;
		var drawingHeight = canvasSize?.Height ?? points.Max(p => p.Y) - minPointY + maxLineWidth;
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

		var offset = canvasSize is null ? new SizeF(minPointX, minPointY) : SizeF.Zero;

		return (image, offset);
	}

	static void DrawStrokes(Canvas canvas, ICollection<PointF> points, float lineWidth, Color strokeColor, SizeF offset)
	{
		using var paint = new APaint
		{
			StrokeWidth = lineWidth,
			StrokeJoin = APaint.Join.Round,
			StrokeCap = APaint.Cap.Round,
			AntiAlias = true
		};

		paint.Color = strokeColor.ToPlatform();
		paint.SetStyle(APaint.Style.Stroke);

		var pointsCount = points.Count;
		if (pointsCount is 1)
		{
			// A single point (a tap) has no segment to draw: with the round cap, DrawPoint draws a circle of the stroke width, as MauiDrawingView draws a tap
			var point = points.First();
			canvas.DrawPoint(point.X - offset.Width, point.Y - offset.Height, paint);
		}

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
		var maxResizeFactor = Math.Min(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);

		var width = Math.Max(maxResizeFactor * sourceSize.Width, 1);
		var height = Math.Max(maxResizeFactor * sourceSize.Height, 1);
		return Bitmap.CreateScaledBitmap(sourceImage, (int)width, (int)height, false)
				?? throw new InvalidOperationException("Failed to create Bitmap");
	}

	static void DrawBackground(Canvas canvas, Paint? brush)
	{
		switch (brush)
		{
			case SolidPaint solidColorBrush:
				canvas.DrawColor(solidColorBrush.Color.ToPlatform(DrawingViewDefaults.BackgroundColor));
				break;
			// Android's gradient shaders need two colors or more: a gradient with a single stop is filled with that color, as iOS, Mac Catalyst and Windows draw it.
			// A gradient without stops only comes from GradientPaint.SetGradientStops (the GradientStops setter replaces an empty array with two white stops):
			// it is filled with the default background.
			case GradientPaint { GradientStops.Length: < 2 } gradientBrush:
				canvas.DrawColor((gradientBrush.GradientStops.FirstOrDefault()?.Color ?? DrawingViewDefaults.BackgroundColor).ToPlatform());
				break;
			case LinearGradientPaint linearGradientBrush:
				{
					var paint = new APaint();
					var colors = new int[linearGradientBrush.GradientStops.Length];
					var positions = new float[linearGradientBrush.GradientStops.Length];
					for (var index = 0; index < linearGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = linearGradientBrush.GradientStops[index];
						colors[index] = gradientStop.Color.ToInt();
						positions[index] = gradientStop.Offset;
					}

					var shader = new LinearGradient(
						(float)linearGradientBrush.StartPoint.X * canvas.Width,
						(float)linearGradientBrush.StartPoint.Y * canvas.Height,
						(float)linearGradientBrush.EndPoint.X * canvas.Width,
						(float)linearGradientBrush.EndPoint.Y * canvas.Height,
						colors,
						positions,
						Shader.TileMode.Clamp ?? throw new NullReferenceException("TileMode is null"));
					paint.SetShader(shader);
					canvas.DrawRect(0, 0, canvas.Width, canvas.Height, paint);

					break;
				}
			case RadialGradientPaint radialGradientBrush:
				{
					var paint = new APaint();
					var colors = new int[radialGradientBrush.GradientStops.Length];
					var positions = new float[radialGradientBrush.GradientStops.Length];
					for (var index = 0; index < radialGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = radialGradientBrush.GradientStops[index];
						colors[index] = gradientStop.Color.ToInt();
						positions[index] = gradientStop.Offset;
					}

					// As in MAUI's canvas, the radius is relative to the larger side of the image, and a radius of 0 spans the diagonal of the image
					// (Android's RadialGradient needs a positive radius)
					var radius = (float)radialGradientBrush.Radius * Math.Max(canvas.Width, canvas.Height);
					if (radius is not > 0)
					{
						radius = (float)Math.Sqrt(((double)canvas.Width * canvas.Width) + ((double)canvas.Height * canvas.Height));
					}

					var shader = new RadialGradient(
						(float)radialGradientBrush.Center.X * canvas.Width,
						(float)radialGradientBrush.Center.Y * canvas.Height,
						radius,
						colors,
						positions,
						Shader.TileMode.Clamp ?? throw new NullReferenceException("TileMode is null"));
					paint.SetShader(shader);
					canvas.DrawRect(0, 0, canvas.Width, canvas.Height, paint);

					break;
				}
			default:
				canvas.DrawColor(DrawingViewDefaults.BackgroundColor.ToPlatform());
				break;
		}
	}
}