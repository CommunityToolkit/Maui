using System.Runtime.InteropServices;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// Drawing view service
/// </summary>
public static class DrawingViewService
{
	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Maximum image size. The image will be resized proportionally.</param>
	/// <param name="background">Image background</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(in IList<IDrawingLine> lines, in Size imageSize, in Paint? background)
	{
		var image = GetUIImageForLines(lines, background);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		return ValueTask.FromResult(GetMaximumUIImage(image, imageSize.Width, imageSize.Height).AsPNG().AsStream());
	}

	/// <summary>
	/// Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="background">Image background</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(in IList<PointF> points,
										in Size imageSize,
										in float lineWidth,
										in Color strokeColor,
										in Paint? background)
	{
		var image = GetUIImageForPoints(points, lineWidth, strokeColor, background);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		return ValueTask.FromResult(GetMaximumUIImage(image, imageSize.Width, imageSize.Height).AsPNG().AsStream());
	}

	static UIImage? GetUIImageForPoints(ICollection<PointF> points,
		NFloat lineWidth,
		Color strokeColor,
		Paint? background)
	{
		return GetUIImage(points, (context, offset) =>
		{
			DrawStrokes(context, points.ToList(), lineWidth, strokeColor, offset);
		}, background, lineWidth);
	}

	static UIImage? GetUIImageForLines(IList<IDrawingLine> lines, in Paint? background)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		var maxLineWidth = lines.Select(x => x.LineWidth).Max();
		return GetUIImage(points, (context, offset) =>
		{
			foreach (var line in lines)
			{
				DrawStrokes(context, line.Points, line.LineWidth, line.LineColor, offset);
			}
		}, background, maxLineWidth);
	}

	static UIImage? GetUIImage(ICollection<PointF> points, Action<CGContext, Size> drawStrokes, Paint? background, NFloat maxLineWidth)
	{
		const int minSize = 1;
		var minPointX = points.Min(p => p.X) - maxLineWidth;
		var minPointY = points.Min(p => p.Y) - maxLineWidth;
		var drawingWidth = points.Max(p => p.X) - minPointX + maxLineWidth;
		var drawingHeight = points.Max(p => p.Y) - minPointY + maxLineWidth;

		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();

		DrawBackground(context, background, imageSize);

		drawStrokes(context, new Size(minPointX, minPointY));

		var image = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return image;
	}

	static void DrawStrokes(CGContext context, IList<PointF> points, NFloat lineWidth, Color strokeColor, Size offset)
	{
		context.SetStrokeColor(strokeColor.ToCGColor());
		context.SetLineWidth(lineWidth);
		context.SetLineCap(CGLineCap.Round);
		context.SetLineJoin(CGLineJoin.Round);

		var (startPointX, startPointY) = points[0];
		context.MoveTo(new NFloat(startPointX), new NFloat(startPointY));
		context.AddLines(points.Select(p => new CGPoint(p.X - offset.Width, p.Y - offset.Height)).ToArray());

		context.StrokePath();
	}

	static UIImage GetMaximumUIImage(UIImage sourceImage, double maxWidth, double maxHeight)
	{
		var sourceSize = sourceImage.Size;
		var maxResizeFactor = Math.Min(maxWidth / sourceSize.Width.Value, maxHeight / sourceSize.Height.Value);

		var width = Math.Max(maxResizeFactor * sourceSize.Width.Value, 1);
		var height = Math.Max(maxResizeFactor * sourceSize.Height.Value, 1);

		UIGraphics.BeginImageContext(new CGSize(width, height));
		sourceImage.Draw(new CGRect(0, 0, width, height));

		var resultImage = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return resultImage;
	}
	static void DrawBackground(CGContext context, Paint? brush, CGSize imageSize)
	{
		switch (brush)
		{
			case SolidPaint solidColorBrush:
				context.SetFillColor(solidColorBrush.Color?.ToCGColor() ?? DrawingViewDefaults.BackgroundColor.AsCGColor());
				context.FillRect(new CGRect(CGPoint.Empty, imageSize));
				break;
			case LinearGradientPaint linearGradientBrush:
				{
					var colors = new CGColor[linearGradientBrush.GradientStops.Length];
					var positions = new NFloat[linearGradientBrush.GradientStops.Length];
					for (var index = 0; index < linearGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = linearGradientBrush.GradientStops[index];
						colors[index] = gradientStop.Color.AsCGColor();
						positions[index] = gradientStop.Offset;
					}

					DrawLinearGradient(
						context,
						imageSize,
						colors,
						positions,
						new CGPoint(linearGradientBrush.StartPoint.X * imageSize.Width, linearGradientBrush.StartPoint.Y * imageSize.Height),
						new CGPoint(linearGradientBrush.EndPoint.X * imageSize.Width, linearGradientBrush.EndPoint.Y * imageSize.Height));
					break;
				}
			case RadialGradientPaint radialGradientBrush:
				{
					var colors = new CGColor[radialGradientBrush.GradientStops.Length];
					var positions = new NFloat[radialGradientBrush.GradientStops.Length];
					for (var index = 0; index < radialGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = radialGradientBrush.GradientStops[index];
						colors[index] = gradientStop.Color.AsCGColor();
						positions[index] = gradientStop.Offset;
					}

					DrawRadialGradient(context, imageSize, colors, positions);
					break;
				}
			default:
				context.SetFillColor(DrawingViewDefaults.BackgroundColor.ToCGColor());
				context.FillRect(new CGRect(CGPoint.Empty, imageSize));
				break;
		}
	}

	static void DrawRadialGradient(CGContext context, CGSize size, CGColor[] colors, NFloat[] locations)
	{
		var colorSpace = CGColorSpace.CreateDeviceRGB();

		var gradient = new CGGradient(colorSpace, colors, locations);
		var path = GetBackgroundPath(size);
		var pathRect = path.BoundingBox;
		var center = new CGPoint(pathRect.GetMidX(), pathRect.GetMidY());
		var radius = Math.Max(pathRect.Size.Width / 2.0, pathRect.Size.Height / 2.0) * Math.Sqrt(2);

		context.SaveState();
		context.AddPath(path);
		context.EOClip();

		context.DrawRadialGradient(gradient, center, 0, center, (NFloat)radius, 0);

		context.RestoreState();
	}


	static void DrawLinearGradient(CGContext context, CGSize size, CGColor[] colors, NFloat[] locations, CGPoint startPoint, CGPoint endPoint)
	{
		var colorSpace = CGColorSpace.CreateDeviceRGB();

		var gradient = new CGGradient(colorSpace, colors, locations);

		context.SaveState();
		context.AddPath(GetBackgroundPath(size));
		context.EOClip();

		context.DrawLinearGradient(
			gradient,
			startPoint,
			endPoint,
			CGGradientDrawingOptions.DrawsBeforeStartLocation);

		context.RestoreState();
	}

	static CGPath GetBackgroundPath(CGSize imageSize)
	{
		var path = new CGPath();

		var rect = new CGRect(0, 0, imageSize.Width, imageSize.Height);
		path.MoveToPoint(rect.GetMinX(), rect.GetMinY());
		path.AddLineToPoint(rect.GetMinX(), rect.GetMaxY());
		path.AddLineToPoint(rect.Width, rect.GetMaxY());
		path.AddLineToPoint(rect.Width, rect.GetMinY());
		path.CloseSubpath();
		return path;
	}
}