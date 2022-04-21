using System.Numerics;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Microsoft.Graphics.Canvas;
using Microsoft.Maui.Platform;
using Point = Windows.Foundation.Point;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
///     Drawing view service
/// </summary>
public static class DrawingViewService
{
	/// <summary>
	///     Get image stream from lines
	/// </summary>
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(IList<IDrawingLine> lines, Size imageSize,
		Color? backgroundColor)
	{
		var canvas = GetImageInternal(lines, imageSize, backgroundColor);
		return GetCanvasRenderTargetStream(canvas);
	}

	/// <summary>
	///     Get image stream from points
	/// </summary>
	/// <param name="points">Drawing points</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="lineWidth">Line Width</param>
	/// <param name="strokeColor">Line color</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(
		IList<PointF> points,
		Size imageSize,
		float lineWidth,
		Color strokeColor,
		Color? backgroundColor)
	{
		var canvas = GetImageInternal(points, imageSize, lineWidth, strokeColor, backgroundColor);
		return GetCanvasRenderTargetStream(canvas);
	}

	static async ValueTask<Stream> GetCanvasRenderTargetStream(CanvasRenderTarget? canvas)
	{
		if (canvas is null)
		{
			return Stream.Null;
		}

		using (canvas)
		{
			var fileStream = new InMemoryRandomAccessStream();
			await canvas.SaveAsync(fileStream, CanvasBitmapFileFormat.Png);

			var stream = fileStream.AsStream();
			stream.Position = 0;

			return stream;
		}
	}

	static CanvasRenderTarget? GetImageInternal(ICollection<PointF> points,
		Size size,
		float lineWidth,
		Color lineColor,
		Color? backgroundColor,
		bool scale = false)
	{
		var (offscreen, offset) = GetCanvasRenderTarget(points, size, scale);
		if (offscreen is null)
		{
			return null;
		}

		using var session = offscreen.CreateDrawingSession();

		session.Clear(backgroundColor?.ToWindowsColor() ?? DrawingViewDefaults.BackgroundColor.ToWindowsColor());

		DrawStrokes(session, points, lineColor, lineWidth, offset);

		return offscreen;
	}

	static void DrawStrokes(CanvasDrawingSession session, 
		IEnumerable<PointF> points, 
		Color lineColor, 
		float lineWidth,
		Size offset)
	{
		var strokeBuilder = new InkStrokeBuilder();
		var inkDrawingAttributes = new InkDrawingAttributes
		{
			Color = lineColor.ToWindowsColor(),
			Size = new Windows.Foundation.Size(lineWidth, lineWidth)
		};
		strokeBuilder.SetDefaultDrawingAttributes(inkDrawingAttributes);
		var strokes = new[]
		{
			strokeBuilder.CreateStroke(points.Select(p => new Point(p.X - offset.Width, p.Y - offset.Height)))
		};
		session.DrawInk(strokes);
	}

	static (CanvasRenderTarget? offscreen, Size offset) GetCanvasRenderTarget(ICollection<PointF> points, Size size, bool scale)
	{
		const int minSize = 1;

		if (points.Count is 0)
		{
			return (null, Size.Zero);
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return (null, new Size(minPointX, minPointY));
		}

		var device = CanvasDevice.GetSharedDevice();
		return (new CanvasRenderTarget(device, scale ? (float)size.Width : drawingWidth, scale ? (float)size.Height : drawingHeight, 96), new Size(minPointX, minPointY));
	}

	static CanvasRenderTarget? GetImageInternal(IList<IDrawingLine> lines, Size size, Color? backgroundColor, bool scale = false)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		var (offscreen, offset) = GetCanvasRenderTarget(points, size, scale);
		if (offscreen is null)
		{
			return null;
		}

		using var session = offscreen.CreateDrawingSession();

		session.Clear(backgroundColor?.ToWindowsColor() ?? DrawingViewDefaults.BackgroundColor.ToWindowsColor());

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
					stroke.DrawingAttributes.Color, (float)stroke.DrawingAttributes.Size.Width);
			}
		}
	}
}