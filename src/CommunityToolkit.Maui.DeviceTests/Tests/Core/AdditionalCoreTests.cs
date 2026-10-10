using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Media;
using CommunityToolkit.Maui.Storage;
using Xunit;

namespace CommunityToolkit.Maui.DeviceTests.Tests.Core;

public class DrawingEventArgsTests
{
	[Fact]
	public void DrawingLineStartedEventArgs_CarriesPoint()
	{
		var point = new PointF(10.5f, 20.5f);
		var args = new DrawingLineStartedEventArgs(point);

		Assert.Equal(point, args.Point);
	}

	[Fact]
	public void MauiDrawingStartedEventArgs_CarriesPoint()
	{
		var point = new PointF(5f, 15f);
		var args = new MauiDrawingStartedEventArgs(point);

		Assert.Equal(point, args.Point);
	}

	[Fact]
	public void MauiOnDrawingEventArgs_CarriesPoint()
	{
		var point = new PointF(100f, 200f);
		var args = new MauiOnDrawingEventArgs(point);

		Assert.Equal(point, args.Point);
	}

	[Fact]
	public void PointDrawnEventArgs_CarriesPoint()
	{
		var point = new PointF(0f, 0f);
		var args = new PointDrawnEventArgs(point);

		Assert.Equal(point, args.Point);
	}

	[Fact]
	public void DrawingLineCompletedEventArgs_CarriesLine()
	{
		var line = new DrawingLine();
		var args = new DrawingLineCompletedEventArgs(line);

		Assert.Same(line, args.LastDrawingLine);
	}
}

public class RatingViewEnumTests
{
	[Theory]
	[InlineData(RatingViewFillOption.Shape, 0)]
	[InlineData(RatingViewFillOption.Background, 1)]
	public void RatingViewFillOption_HasExpectedValues(RatingViewFillOption option, int expected)
	{
		Assert.Equal(expected, (int)option);
	}

	[Theory]
	[InlineData(RatingViewShape.Star, 0)]
	[InlineData(RatingViewShape.Heart, 1)]
	[InlineData(RatingViewShape.Circle, 2)]
	[InlineData(RatingViewShape.Like, 3)]
	[InlineData(RatingViewShape.Dislike, 4)]
	[InlineData(RatingViewShape.Custom, 5)]
	public void RatingViewShape_HasExpectedValues(RatingViewShape shape, int expected)
	{
		Assert.Equal(expected, (int)shape);
	}

	[Fact]
	public void RatingViewShape_HasSixValues()
	{
		Assert.Equal(6, Enum.GetValues<RatingViewShape>().Length);
	}

	[Fact]
	public void RatingViewFillOption_HasTwoValues()
	{
		Assert.Equal(2, Enum.GetValues<RatingViewFillOption>().Length);
	}
}

public class ToastDurationEnumTests
{
	[Theory]
	[InlineData(ToastDuration.Short, 0)]
	[InlineData(ToastDuration.Long, 1)]
	public void ToastDuration_HasExpectedValues(ToastDuration duration, int expected)
	{
		Assert.Equal(expected, (int)duration);
	}
}

public class MauiDrawingLineTests
{
	[Fact]
	public void MauiDrawingLine_DefaultLineWidth()
	{
		var line = new MauiDrawingLine();

		Assert.Equal(5f, line.LineWidth);
	}

	[Fact]
	public void MauiDrawingLine_DefaultLineColor()
	{
		var line = new MauiDrawingLine();

		Assert.Equal(Colors.Black, line.LineColor);
	}

	[Fact]
	public void MauiDrawingLine_DefaultPoints_IsEmpty()
	{
		var line = new MauiDrawingLine();

		Assert.NotNull(line.Points);
		Assert.Empty(line.Points);
	}

	[Fact]
	public void MauiDrawingLine_DefaultGranularity()
	{
		var line = new MauiDrawingLine();

		Assert.Equal(5, line.Granularity);
	}

	[Fact]
	public void MauiDrawingLine_DefaultShouldSmoothPathWhenDrawn()
	{
		var line = new MauiDrawingLine();

		Assert.True(line.ShouldSmoothPathWhenDrawn);
	}

	[Fact]
	public void MauiDrawingLine_CanSetProperties()
	{
		var line = new MauiDrawingLine
		{
			LineWidth = 10f,
			LineColor = Colors.Red,
			Granularity = 20,
			ShouldSmoothPathWhenDrawn = false,
		};

		Assert.Equal(10f, line.LineWidth);
		Assert.Equal(Colors.Red, line.LineColor);
		Assert.Equal(20, line.Granularity);
		Assert.False(line.ShouldSmoothPathWhenDrawn);
	}

	[Theory]
	[InlineData(0, 5)]
	[InlineData(-10, 5)]
	[InlineData(3, 5)]
	[InlineData(5, 5)]
	[InlineData(100, 100)]
	public void MauiDrawingLine_Granularity_ClampsToMinimum(int input, int expected)
	{
		var line = new MauiDrawingLine
		{
			Granularity = input
		};

		Assert.Equal(expected, line.Granularity);
	}

	[Fact]
	public void MauiDrawingLine_CanAddPoints()
	{
		var line = new MauiDrawingLine();
		line.Points.Add(new PointF(1, 2));
		line.Points.Add(new PointF(3, 4));

		Assert.Equal(2, line.Points.Count);
	}
}

public class ImageOptionsTests
{
	[Fact]
	public void ImagePointOptions_StoresProperties()
	{
		var points = new List<PointF> { new(0, 0), new(10, 10) };
		var size = new Size(100, 200);
		var options = new ImagePointOptions(points, size, 3f, Colors.Blue, null, null);

		Assert.Equal(2, options.Points.Count);
		Assert.Equal(size, options.DesiredSize);
		Assert.Equal(3f, options.LineWidth);
		Assert.Equal(Colors.Blue, options.StrokeColor);
		Assert.Null(options.Background);
		Assert.Null(options.CanvasSize);
	}

	[Fact]
	public void ImagePointOptions_WithBackground()
	{
		var points = new List<PointF> { new(5, 5) };
		var background = new SolidColorBrush(Colors.White);
		var options = new ImagePointOptions(points, new Size(50, 50), 1f, Colors.Black, background, new Size(200, 200));

		Assert.NotNull(options.Background);
		Assert.Equal(new Size(200, 200), options.CanvasSize);
	}

	[Fact]
	public void ImageLineOptions_JustLines()
	{
		var lines = new List<IDrawingLine> { new DrawingLine() };
		var options = ImageLineOptions.JustLines(lines, new Size(100, 100), null);

		Assert.Single(options.Lines);
		Assert.Equal(new Size(100, 100), options.DesiredSize);
		Assert.Null(options.Background);
		Assert.Null(options.CanvasSize);
	}

	[Fact]
	public void ImageLineOptions_FullCanvas()
	{
		var lines = new List<IDrawingLine> { new DrawingLine(), new DrawingLine() };
		var canvasSize = new Size(500, 500);
		var options = ImageLineOptions.FullCanvas(lines, new Size(100, 100), null, canvasSize);

		Assert.Equal(2, options.Lines.Count);
		Assert.Equal(canvasSize, options.CanvasSize);
	}
}

public class EnsureSuccessTests
{
	[Fact]
	public void FolderPickerResult_EnsureSuccess_DoesNotThrow_WhenSuccessful()
	{
		var result = new FolderPickerResult(new Folder("/test", "Test"), null);

		result.EnsureSuccess();
	}

	[Fact]
	public void FolderPickerResult_EnsureSuccess_Throws_WhenFailed()
	{
		var exception = new FolderPickerException("Pick failed");
		var result = new FolderPickerResult(null, exception);

		FolderPickerException? caught = null;
		try
		{
			result.EnsureSuccess();
		}
		catch (FolderPickerException ex)
		{
			caught = ex;
		}

		Assert.NotNull(caught);
		Assert.Equal("Pick failed", caught.Message);
	}

	[Fact]
	public void FileSaverResult_EnsureSuccess_DoesNotThrow_WhenSuccessful()
	{
		var result = new FileSaverResult("/path/to/file.txt", null);

		result.EnsureSuccess();
	}

	[Fact]
	public void FileSaverResult_EnsureSuccess_Throws_WhenFailed()
	{
		var exception = new FileSaveException("Save failed");
		var result = new FileSaverResult(null, exception);

		FileSaveException? caught = null;
		try
		{
			result.EnsureSuccess();
		}
		catch (FileSaveException ex)
		{
			caught = ex;
		}

		Assert.NotNull(caught);
		Assert.Equal("Save failed", caught.Message);
	}

	[Fact]
	public void SpeechToTextResult_EnsureSuccess_DoesNotThrow_WhenSuccessful()
	{
		var result = new SpeechToTextResult("Hello world", null);

		result.EnsureSuccess();
	}

	[Fact]
	public void SpeechToTextResult_EnsureSuccess_Throws_WhenFailed()
	{
		var exception = new InvalidOperationException("Recognition failed");
		var result = new SpeechToTextResult(null, exception);

		InvalidOperationException? caught = null;
		try
		{
			result.EnsureSuccess();
		}
		catch (InvalidOperationException ex)
		{
			caught = ex;
		}

		Assert.NotNull(caught);
		Assert.Equal("Recognition failed", caught.Message);
	}
}

