using System.Runtime.InteropServices;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

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
	public static ValueTask<Stream> GetImageStream(in IList<IDrawingLine> lines, in Size imageSize, in Color? backgroundColor)
	{
		var image = GetUIImageForLines(lines, backgroundColor);
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
	/// <param name="backgroundColor">Image background color</param>
	/// <returns>Image stream</returns>
	public static ValueTask<Stream> GetImageStream(in IList<PointF> points,
										in Size imageSize,
										in float lineWidth,
										in Color strokeColor,
										in Color? backgroundColor)
	{
		var image = GetUIImageForPoints(points, lineWidth, strokeColor, backgroundColor);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		return ValueTask.FromResult(GetMaximumUIImage(image, imageSize.Width, imageSize.Height).AsPNG().AsStream());
	}

	static UIImage? GetUIImageForPoints(ICollection<PointF> points,
		NFloat lineWidth,
		Color strokeColor,
		Color? backgroundColor)
	{
		return GetUIImage(points, (context, offset) =>
		{
			DrawStrokes(context, points.ToList(), lineWidth, strokeColor, offset);
		}, backgroundColor);
	}

	static UIImage? GetUIImageForLines(IList<IDrawingLine> lines, in Color? backgroundColor)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		return GetUIImage(points, (context, offset) =>
		{
			foreach (var line in lines)
			{
				DrawStrokes(context, line.Points, line.LineWidth, line.LineColor, offset);
			}			
		}, backgroundColor);
	}

	static UIImage? GetUIImage(ICollection<PointF> points, Action<CGContext, Size> drawStrokes, Color? backgroundColor)
	{
		const int minSize = 1;
		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;

		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();

		context.SetFillColor(backgroundColor?.ToCGColor() ?? DrawingViewDefaults.BackgroundColor.ToCGColor());
		context.FillRect(new CGRect(CGPoint.Empty, imageSize));

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
		var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width.Value, maxHeight / sourceSize.Height.Value);
		if (maxResizeFactor > 1)
		{
			return sourceImage;
		}

		var width = maxResizeFactor * sourceSize.Width.Value;
		var height = maxResizeFactor * sourceSize.Height.Value;

		UIGraphics.BeginImageContext(new CGSize(width, height));
		sourceImage.Draw(new CGRect(0, 0, width, height));

		var resultImage = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return resultImage;
	}
}