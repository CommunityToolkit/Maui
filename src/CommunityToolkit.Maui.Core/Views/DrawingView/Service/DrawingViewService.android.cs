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

		if (OperatingSystem.IsAndroidVersionAtLeast(29))
		{
			paint.Color = strokeColor.ToPlatform();
		}

		paint.SetStyle(APaint.Style.Stroke);

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

					var shader = new RadialGradient(
						(float)radialGradientBrush.Center.X * canvas.Width,
						(float)radialGradientBrush.Center.Y * canvas.Height,
						(float)radialGradientBrush.Radius * canvas.Width,
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