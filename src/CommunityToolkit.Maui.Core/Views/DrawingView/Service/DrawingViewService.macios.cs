﻿using CommunityToolkit.Maui.Core.Views;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

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

		var resizedImage = MaxResizeImage(image, (float)imageSize.Width, (float)imageSize.Height);
		return resizedImage.AsJPEG().AsStream();
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
	public static Stream GetImageStream(IList<Point>? points,
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

		var resizedImage = MaxResizeImage(image, (float)imageSize.Width, (float)imageSize.Height);
		return resizedImage.AsJPEG().AsStream();
	}

	static UIImage? GetImageInternal(IList<Point> points,
		float lineWidth,
		Color strokeColor,
		Color backgroundColor)
	{
		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		const int minSize = 1;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();
		if (context is null)
		{
			throw new Exception("Current Context is null");
		}

		context.SetFillColor(backgroundColor.ToCGColor());
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

	static UIImage? GetImageInternal(IList<ILine> lines,
		Color backgroundColor)
	{
		var points = lines.SelectMany(x => x.Points).ToList();
		var minPointX = points.Min(p => p.X);
		var minPointY = points.Min(p => p.Y);
		var drawingWidth = points.Max(p => p.X) - minPointX;
		var drawingHeight = points.Max(p => p.Y) - minPointY;
		const int minSize = 1;
		if (drawingWidth < minSize || drawingHeight < minSize)
		{
			return null;
		}

		var imageSize = new CGSize(drawingWidth, drawingHeight);
		UIGraphics.BeginImageContextWithOptions(imageSize, false, 1);

		var context = UIGraphics.GetCurrentContext();
		if (context is null)
		{
			throw new Exception("Current Context is null");
		}

		context.SetFillColor(backgroundColor.ToCGColor());
		context.FillRect(new CGRect(CGPoint.Empty, imageSize));
		foreach (var line in lines)
		{
			context.SetStrokeColor(line.LineColor.ToCGColor());
			context.SetLineWidth(line.LineWidth);
			context.SetLineCap(CGLineCap.Round);
			context.SetLineJoin(CGLineJoin.Round);

			var startPoint = line.Points.First();
			context.MoveTo((float)startPoint.X, (float)startPoint.Y);
			context.AddLines(line.Points.Select(p => new CGPoint(p.X - minPointX, p.Y - minPointY)).ToArray());
		}

		context.StrokePath();
		var image = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();
		return image;
	}

	static UIImage MaxResizeImage(UIImage sourceImage, float maxWidth, float maxHeight)
	{
		var sourceSize = sourceImage.Size;
		var maxResizeFactor = Math.Max(maxWidth / sourceSize.Width, maxHeight / sourceSize.Height);
		if (maxResizeFactor > 1)
		{
			return sourceImage;
		}

		var width = maxResizeFactor * sourceSize.Width;
		var height = maxResizeFactor * sourceSize.Height;
		UIGraphics.BeginImageContext(new CGSize(width, height));
		sourceImage.Draw(new CGRect(0, 0, width, height));
		var resultImage = UIGraphics.GetImageFromCurrentImageContext();
		UIGraphics.EndImageContext();
		return resultImage;
	}
}