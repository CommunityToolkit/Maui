using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Maui.Platform;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using Size = Microsoft.Maui.Graphics.Size;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
///     Drawing view service
/// </summary>
public static partial class DrawingViewService
{
	static readonly CanvasStrokeStyle roundStartEndCapCanvasStrokeStyle = new()
	{
		StartCap = CanvasCapStyle.Round,
		EndCap = CanvasCapStyle.Round,
	};

	/// <summary>
	/// Get image stream from lines
	/// </summary>
	/// <param name="options">The options controlling how the resulting image is generated.</param>
	/// <param name="token"><see cref="CancellationToken"/></param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetPlatformImageStream(ImageLineOptions options, CancellationToken token = default)
	{
		token.ThrowIfCancellationRequested();

		var canvas = GetImageInternal(options.Lines, options.Background, options.CanvasSize);

		return GetCanvasRenderTargetStream(canvas, options.DesiredSize, token);
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

		var canvas = GetImageInternal(options.Points, options.LineWidth, options.StrokeColor, options.Background, options.CanvasSize);

		return GetCanvasRenderTargetStream(canvas, options.DesiredSize, token);
	}

	static async ValueTask<Stream> GetCanvasRenderTargetStream(CanvasRenderTarget? canvas, Size desiredSize, CancellationToken token)
	{
		if (canvas is null)
		{
			return Stream.Null;
		}

		using (canvas)
		using (var image = GetMaximumCanvasRenderTarget(canvas, desiredSize))
		{
			var fileStream = new InMemoryRandomAccessStream();
			await image.SaveAsync(fileStream, CanvasBitmapFileFormat.Png).AsTask(token);

			var stream = fileStream.AsStream();
			stream.Position = 0;

			return stream;
		}
	}

	static CanvasRenderTarget GetMaximumCanvasRenderTarget(CanvasRenderTarget sourceImage, Size maxSize)
	{
		var sourceSize = sourceImage.Size;
		var maxResizeFactor = Math.Min(maxSize.Width / sourceSize.Width, maxSize.Height / sourceSize.Height);

		var width = Math.Max(maxResizeFactor * sourceSize.Width, 1);
		var height = Math.Max(maxResizeFactor * sourceSize.Height, 1);

		var resizedImage = new CanvasRenderTarget(sourceImage.Device, (float)width, (float)height, 96);
		using var session = resizedImage.CreateDrawingSession();
		session.DrawImage(sourceImage, new Rect(0, 0, width, height));

		return resizedImage;
	}

	static CanvasRenderTarget? GetImageInternal(ICollection<PointF> points,
		float lineWidth,
		Color lineColor,
		Paint? background,
		Size? canvasSize)
	{
		var (offscreen, offset) = GetCanvasRenderTarget(points, lineWidth, canvasSize);
		if (offscreen is null)
		{
			return null;
		}

		using var session = offscreen.CreateDrawingSession();
		var brush = GetCanvasBrush(session, background, offscreen.Size);
		session.FillRectangle(new Rect(0, 0, offscreen.Size.Width, offscreen.Size.Height), brush);

		DrawStrokes(session, points, lineColor, lineWidth, offset);

		return offscreen;
	}

	static void DrawStrokes(CanvasDrawingSession session,
		IEnumerable<PointF> points,
		Color lineColor,
		float lineWidth,
		Size offset)
	{
		var strokePoints = points.Select(p => new Point(p.X - offset.Width, p.Y - offset.Height)).ToList();
		if (strokePoints.Count is 0)
		{
			// InkStrokeBuilder.CreateStroke needs points
			return;
		}

		if (strokePoints.Count is 1)
		{
			// DrawInk only draws segments between two points, so a single point (a tap) is filled as a dot of the line width, as MauiDrawingView draws it
			session.FillCircle(new Vector2((float)strokePoints[0].X, (float)strokePoints[0].Y), lineWidth / 2, lineColor.ToWindowsColor());
			return;
		}

		var strokeBuilder = new InkStrokeBuilder();
		var inkDrawingAttributes = new InkDrawingAttributes
		{
			Color = lineColor.ToWindowsColor(),
			Size = new Windows.Foundation.Size(lineWidth, lineWidth)
		};
		strokeBuilder.SetDefaultDrawingAttributes(inkDrawingAttributes);
		var strokes = new[]
		{
			strokeBuilder.CreateStroke(strokePoints)
		};
		session.DrawInk(strokes);
	}

	static (CanvasRenderTarget? offscreen, Size offset) GetCanvasRenderTarget(ICollection<PointF> points, float maxLineWidth, Size? canvasSize)
	{
		const int minSize = 1;

		if (points.Count is 0)
		{
			return (null, Size.Zero);
		}

		var minPointX = points.Min(p => p.X) - maxLineWidth;
		var minPointY = points.Min(p => p.Y) - maxLineWidth;
		var drawingWidth = (float?)canvasSize?.Width ?? points.Max(p => p.X) - minPointX + maxLineWidth;
		var drawingHeight = (float?)canvasSize?.Height ?? points.Max(p => p.Y) - minPointY + maxLineWidth;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return (null, new Size(minPointX, minPointY));
		}

		var device = CanvasDevice.GetSharedDevice();
		var offset = canvasSize is null ? new Size(minPointX, minPointY) : Size.Zero;
		return (new CanvasRenderTarget(device, drawingWidth, drawingHeight, 96), offset);
	}

	static CanvasRenderTarget? GetImageInternal(IList<IDrawingLine> lines, Paint? background, Size? canvasSize)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		if (points.Count is 0)
		{
			return null;
		}

		var maxLineWidth = lines.Select(x => x.LineWidth).Max();
		var (offscreen, offset) = GetCanvasRenderTarget(points, maxLineWidth, canvasSize);
		if (offscreen is null)
		{
			return null;
		}

		using var session = offscreen.CreateDrawingSession();

		var brush = GetCanvasBrush(session, background, offscreen.Size);

		session.FillRectangle(new Rect(0, 0, offscreen.Size.Width, offscreen.Size.Height), brush);

		foreach (var line in lines)
		{
			DrawStrokes(session, line.Points, line.LineColor, line.LineWidth, offset);
		}

		return offscreen;
	}

	static void DrawInk(this CanvasDrawingSession session, IEnumerable<InkStroke> strokes)
	{
		foreach (var stroke in strokes)
		{
			var inkPoints = stroke.GetInkPoints();
			for (var index = 0; index < inkPoints.Count - 1; index++)
			{
				var currentPoint = inkPoints[index].Position;
				var nextPoint = inkPoints[index + 1].Position;
				session.DrawLine(
					new Vector2((float)currentPoint.X, (float)currentPoint.Y),
					new Vector2((float)nextPoint.X, (float)nextPoint.Y),
					stroke.DrawingAttributes.Color, (float)stroke.DrawingAttributes.Size.Width,
					roundStartEndCapCanvasStrokeStyle);
			}
		}
	}

	static ICanvasBrush GetCanvasBrush(ICanvasResourceCreator canvasResourceCreator, Paint? brush, Windows.Foundation.Size size)
	{
		switch (brush)
		{
			case SolidPaint solidColorBrush:
				return new CanvasSolidColorBrush(canvasResourceCreator, (solidColorBrush.Color ?? DrawingViewDefaults.BackgroundColor).ToWindowsColor());
			// A gradient without stops only comes from GradientPaint.SetGradientStops (the GradientStops setter replaces an empty array with two white
			// stops): it is filled with the default background, as on Android, iOS and Mac Catalyst.
			case GradientPaint { GradientStops.Length: 0 }:
				return new CanvasSolidColorBrush(canvasResourceCreator, DrawingViewDefaults.BackgroundColor.ToWindowsColor());
			case LinearGradientPaint linearGradientBrush:
				{
					var gradientStops = new CanvasGradientStop[linearGradientBrush.GradientStops.Length];
					for (var index = 0; index < linearGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = linearGradientBrush.GradientStops[index];
						gradientStops[index] = new CanvasGradientStop(gradientStop.Offset, gradientStop.Color.ToWindowsColor());
					}

					return new CanvasLinearGradientBrush(canvasResourceCreator, gradientStops)
					{
						StartPoint = new Vector2((float)(linearGradientBrush.StartPoint.X * size.Width), (float)(linearGradientBrush.StartPoint.Y * size.Height)),
						EndPoint = new Vector2((float)(linearGradientBrush.EndPoint.X * size.Width), (float)(linearGradientBrush.EndPoint.Y * size.Height))
					};
				}
			case RadialGradientPaint radialGradientBrush:
				{
					var gradientStops = new CanvasGradientStop[radialGradientBrush.GradientStops.Length];
					for (var index = 0; index < radialGradientBrush.GradientStops.Length; index++)
					{
						var gradientStop = radialGradientBrush.GradientStops[index];
						gradientStops[index] = new CanvasGradientStop(gradientStop.Offset, gradientStop.Color.ToWindowsColor());
					}

					// As in MAUI's canvas, a circle whose radius is relative to the larger side of the image; a radius of 0 spans the diagonal of the image,
					// as on Android, iOS and Mac Catalyst
					var radius = radialGradientBrush.Radius * Math.Max(size.Width, size.Height);
					if (radius is not > 0)
					{
						radius = Math.Sqrt((size.Width * size.Width) + (size.Height * size.Height));
					}

					return new CanvasRadialGradientBrush(canvasResourceCreator, gradientStops)
					{
						Center = new Vector2((float)(radialGradientBrush.Center.X * size.Width), (float)(radialGradientBrush.Center.Y * size.Height)),
						RadiusX = (float)radius,
						RadiusY = (float)radius
					};
				}
			default:
				return new CanvasSolidColorBrush(canvasResourceCreator, DrawingViewDefaults.BackgroundColor.ToWindowsColor());
		}
	}
}