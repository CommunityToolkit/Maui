using System.Numerics;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
using Microsoft.Graphics.Canvas;
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
	/// <param name="lines">Drawing lines</param>
	/// <param name="imageSize">Image size</param>
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static async ValueTask<Stream> GetImageStream(IList<DrawingLine> lines, Size imageSize, Color? backgroundColor)
	{
		var image = GetImageInternal(lines, imageSize, backgroundColor);
		if (image is null)
		{
			return Stream.Null;
		}

		using (image)
		{
			var fileStream = new InMemoryRandomAccessStream();
			await image.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg);

			var stream = fileStream.AsStream();
			stream.Position = 0;

			return stream;
		}
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
	public static async ValueTask<Stream> GetImageStream(
		IList<PointF> points,
		Size imageSize,
		float lineWidth,
		Color strokeColor,
		Color? backgroundColor)
	{
		if (points.Count < 2)
		{
			return Stream.Null;
		}

		var image = GetImageInternal(points, imageSize, lineWidth, strokeColor, backgroundColor);
		if (image is null)
		{
			return Stream.Null;
		}

		using (image)
		{
			var fileStream = new InMemoryRandomAccessStream();
			await image.SaveAsync(fileStream, CanvasBitmapFileFormat.Jpeg);

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
		const int minSize = 1;

		if (points.Count is 0)
		{
			return null;
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var device = CanvasDevice.GetSharedDevice();
		var offscreen = new CanvasRenderTarget(device, scale ? (int)size.Width : drawingWidth, scale ? (int)size.Height : drawingHeight, 96);

		using var session = offscreen.CreateDrawingSession();
		session.Clear(backgroundColor?.ToWindowsColor() ?? Defaults.BackgroundColor.ToWindowsColor());
		var strokeBuilder = new InkStrokeBuilder();
		var inkDrawingAttributes = new InkDrawingAttributes
		{
			Color = lineColor.ToWindowsColor(),
			Size = new Windows.Foundation.Size(lineWidth, lineWidth)
		};
		strokeBuilder.SetDefaultDrawingAttributes(inkDrawingAttributes);
		var strokes = new[]
		{
			strokeBuilder.CreateStroke(points.Select(p => new Windows.Foundation.Point((p.X - minPointX)*offscreen.Size.Width/drawingWidth, (p.Y - minPointY)*offscreen.Size.Height/drawingHeight)))
		};
		session.DrawInk(strokes);

		return offscreen;
	}

	static CanvasRenderTarget? GetImageInternal(IList<DrawingLine> lines, Size size, Color? backgroundColor, bool scale = false)
	{
		const int minSize = 1;

		var points = lines.SelectMany(x => x.Points).ToList();
		if (points.Count is 0)
		{
			return null;
		}

		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;

		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}


		var device = CanvasDevice.GetSharedDevice();
		var offscreen = new CanvasRenderTarget(device, scale ? (int)size.Width : drawingWidth, scale ? (int)size.Height : drawingHeight, 96);

		using var session = offscreen.CreateDrawingSession();
		session.Clear(backgroundColor?.ToWindowsColor() ?? Defaults.BackgroundColor.ToWindowsColor());

		foreach (var line in lines)
		{
			var strokeBuilder = new InkStrokeBuilder();
			var inkDrawingAttributes = new InkDrawingAttributes
			{
				Color = line.LineColor.ToWindowsColor(),
				Size = new Windows.Foundation.Size(line.LineWidth, line.LineWidth)
			};
			strokeBuilder.SetDefaultDrawingAttributes(inkDrawingAttributes);
			var strokes = new[]
			{
				strokeBuilder.CreateStroke(line.Points.Select(p => new Windows.Foundation.Point((p.X - minPointX)*offscreen.Size.Width/drawingWidth, (p.Y - minPointY)*offscreen.Size.Height/drawingHeight)))
			};

			session.DrawInk(strokes);
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