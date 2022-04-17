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

		var resizedImage = GetMaximumUIImage(image, imageSize.Width, imageSize.Height);
		return ValueTask.FromResult(resizedImage.AsJPEG().AsStream());
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

		var image = GetUIImageForPoints(points, lineWidth, strokeColor, backgroundColor);
		if (image is null)
		{
			return ValueTask.FromResult(Stream.Null);
		}

		var resizedImage = GetMaximumUIImage(image, (float)imageSize.Width, (float)imageSize.Height);
		return ValueTask.FromResult(resizedImage.AsJPEG().AsStream());
	}

	static UIImage? GetUIImageForPoints(ICollection<PointF> points,
		NFloat lineWidth,
		Color strokeColor,
		Color? backgroundColor)
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

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();
		context.SetFillColor(backgroundColor?.ToCGColor() ?? Defaults.BackgroundColor.ToCGColor());
		context.FillRect(new CGRect(CGPoint.Empty, imageSize));

		context.SetStrokeColor(strokeColor.ToCGColor());
		context.SetLineWidth(lineWidth);
		context.SetLineCap(CGLineCap.Round);
		context.SetLineJoin(CGLineJoin.Round);

		context.AddLines(points.Select(p => new CGPoint(p.X - minPointX, p.Y - minPointY)).ToArray());
		context.StrokePath();

		var image = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return image;
	}

	static UIImage? GetUIImageForLines(in IList<IDrawingLine> lines, in Color? backgroundColor)
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

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();

		context.SetFillColor(backgroundColor?.ToCGColor() ?? Defaults.BackgroundColor.ToCGColor());
		context.FillRect(new CGRect(CGPoint.Empty, imageSize));

		foreach (var line in lines)
		{
			context.SetStrokeColor(line.LineColor.ToCGColor());
			context.SetLineWidth(line.LineWidth);
			context.SetLineCap(CGLineCap.Round);
			context.SetLineJoin(CGLineJoin.Round);

			var (startPointX, startPointY) = line.Points[0];
			context.MoveTo(new NFloat(startPointX), new NFloat(startPointY));
			context.AddLines(line.Points.Select(p => new CGPoint(p.X - minPointX, p.Y - minPointY)).ToArray());
		}

		context.StrokePath();

		var image = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();

		return image;
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