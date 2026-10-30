using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Platform;

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
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImageLineOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		var image = GetUIImageForLines(options.Lines, options.Background, options.CanvasSize);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		token.ThrowIfCancellationRequested();

		var imageAsPng = GetMaximumUIImage(image, options.DesiredSize.Width, options.DesiredSize.Height)
							.AsPNG() ?? throw new InvalidOperationException("Unable to convert image to PNG");

		return ValueTask.FromResult(imageAsPng.AsStream());
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

		var imageSize = options.DesiredSize;
		var canvasSize = options.CanvasSize;

		var image = GetUIImageForPoints(options.Points, options.LineWidth, options.StrokeColor, options.Background, canvasSize);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		token.ThrowIfCancellationRequested();

		var imageAsPng = GetMaximumUIImage(image, imageSize.Width, imageSize.Height)
							.AsPNG() ?? throw new InvalidOperationException("Unable to convert image to PNG");

		return ValueTask.FromResult(imageAsPng.AsStream());
	}

	static UIImage? GetUIImageForPoints(ICollection<PointF> points,
		nfloat lineWidth,
		Color strokeColor,
		Paint? background,
		Size? canvasSize = null)
	{
		return GetUIImage(points, (context, offset) =>
		{
			DrawStrokes(context, points.ToList(), lineWidth, strokeColor, offset);
		}, background, lineWidth, canvasSize);
	}

	static UIImage? GetUIImageForLines(IList<IDrawingLine> lines, in Paint? background, Size? canvasSize = null)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		var drawingLineWithLargestLineWidth = lines.MaxBy(x => x.LineWidth);

		if (drawingLineWithLargestLineWidth is null)
		{
			// GetPlatformImageStream returns Stream.Null when there is nothing to draw, as on every platform
			return null;
		}

		return GetUIImage(points, (context, offset) =>
		{
			foreach (var line in lines)
			{
				DrawStrokes(context, line.Points, line.LineWidth, line.LineColor, offset);
			}
		}, background, drawingLineWithLargestLineWidth.LineWidth, canvasSize);
	}

	static UIImage? GetUIImage(ICollection<PointF> points, Action<CGContext, Size> drawStrokes, Paint? background, nfloat maxLineWidth, Size? canvasSize)
	{
		const int minSize = 1;
		if (points.Count is 0)
		{
			return null;
		}

		var minPointX = points.Min(p => p.X) - maxLineWidth;
		var minPointY = points.Min(p => p.Y) - maxLineWidth;
		var drawingWidth = canvasSize?.Width ?? points.Max(p => p.X) - minPointX + maxLineWidth;
		var drawingHeight = canvasSize?.Height ?? points.Max(p => p.Y) - minPointY + maxLineWidth;

		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();

		if (context is null)
		{
			UIGraphics.EndImageContext();
			return null;
		}

		DrawBackground(context, background, imageSize);

		var offset = canvasSize is null ? new Size(minPointX, minPointY) : Size.Zero;

		drawStrokes(context, offset);

		var image = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return image;
	}

	static void DrawStrokes(CGContext context, IList<PointF> points, nfloat lineWidth, Color strokeColor, Size offset)
	{
		if (points.Count is 0)
		{
			return;
		}

		context.SetStrokeColor(strokeColor.ToCGColor());
		context.SetLineWidth(lineWidth);
		context.SetLineCap(CGLineCap.Round);
		context.SetLineJoin(CGLineJoin.Round);

		var (startPointX, startPointY) = points[0];
		context.MoveTo(new nfloat(startPointX), new nfloat(startPointY));
		var linePoints = points.Select(p => new CGPoint(p.X - offset.Width, p.Y - offset.Height)).ToArray();
		if (linePoints.Length is 1)
		{
			// AddLines with one point only moves to it, so a single point (a tap) becomes a zero-length segment, which the round caps draw as a dot
			// of the line width, as MauiDrawingView draws it
			linePoints = [linePoints[0], linePoints[0]];
		}

		context.AddLines(linePoints);

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

		return resultImage ?? throw new InvalidOperationException("Unable to create the resized image.");
	}
	static void DrawBackground(CGContext context, Paint? brush, CGSize imageSize)
	{
		switch (brush)
		{
			case SolidPaint solidColorBrush:
				context.SetFillColor(solidColorBrush.Color?.ToCGColor() ?? DrawingViewDefaults.BackgroundColor.AsCGColor());
				context.FillRect(new CGRect(CGPoint.Empty, imageSize));
				break;
			// CGGradient needs one color or more. A gradient without stops only comes from GradientPaint.SetGradientStops (the GradientStops setter
			// replaces an empty array with two white stops): it is filled with the default background, as on Android.
			case GradientPaint { GradientStops.Length: 0 }:
				context.SetFillColor(DrawingViewDefaults.BackgroundColor.ToCGColor());
				context.FillRect(new CGRect(CGPoint.Empty, imageSize));
				break;
			case LinearGradientPaint linearGradientBrush:
				{
					var colors = new CGColor[linearGradientBrush.GradientStops.Length];
					var positions = new nfloat[linearGradientBrush.GradientStops.Length];
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
					var positions = new nfloat[radialGradientBrush.GradientStops.Length];
					for (var index = 0; index < radialGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = radialGradientBrush.GradientStops[index];
						colors[index] = gradientStop.Color.AsCGColor();
						positions[index] = gradientStop.Offset;
					}

					// As in MAUI's canvas, the radius is relative to the larger side of the image, and a radius of 0 spans its diagonal
					var radius = radialGradientBrush.Radius * Math.Max(imageSize.Width.Value, imageSize.Height.Value);
					if (radius is not > 0)
					{
						radius = Math.Sqrt((imageSize.Width.Value * imageSize.Width.Value) + (imageSize.Height.Value * imageSize.Height.Value));
					}

					DrawRadialGradient(
						context,
						imageSize,
						colors,
						positions,
						new CGPoint(radialGradientBrush.Center.X * imageSize.Width, radialGradientBrush.Center.Y * imageSize.Height),
						radius);
					break;
				}
			default:
				context.SetFillColor(DrawingViewDefaults.BackgroundColor.ToCGColor());
				context.FillRect(new CGRect(CGPoint.Empty, imageSize));
				break;
		}
	}

	static void DrawRadialGradient(CGContext context, CGSize size, CGColor[] colors, nfloat[] locations, CGPoint center, double radius)
	{
		var colorSpace = CGColorSpace.CreateDeviceRGB();

		var gradient = new CGGradient(colorSpace, colors, locations);

		context.SaveState();
		context.AddPath(GetBackgroundPath(size));
		context.EOClip();

		// The end color continues beyond the radius, as in MAUI's canvas and on Android
		context.DrawRadialGradient(gradient, center, 0, center, (nfloat)radius, CGGradientDrawingOptions.DrawsBeforeStartLocation | CGGradientDrawingOptions.DrawsAfterEndLocation);

		context.RestoreState();
	}


	static void DrawLinearGradient(CGContext context, CGSize size, CGColor[] colors, nfloat[] locations, CGPoint startPoint, CGPoint endPoint)
	{
		var colorSpace = CGColorSpace.CreateDeviceRGB();

		var gradient = new CGGradient(colorSpace, colors, locations);

		context.SaveState();
		context.AddPath(GetBackgroundPath(size));
		context.EOClip();

		// The start and end colors continue beyond the start and end points, as in MAUI's canvas and on Android
		context.DrawLinearGradient(
			gradient,
			startPoint,
			endPoint,
			CGGradientDrawingOptions.DrawsBeforeStartLocation | CGGradientDrawingOptions.DrawsAfterEndLocation);

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