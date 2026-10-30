using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class ImageOptionsTests : BaseViewTest
{
	// The image is resized by Math.Min(desired width / width, desired height / height): these sizes make that factor NaN or infinite
	[Theory]
	[InlineData(double.NaN, 10)]
	[InlineData(10, double.NaN)]
	[InlineData(double.NaN, double.PositiveInfinity)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity)]
	public void ImagePointOptionsRejectsADesiredSizeWithANaNSideOrTwoInfiniteSides(double width, double height)
	{
		var exception = Assert.Throws<ArgumentException>(() => new ImagePointOptions([new PointF(1, 1)], new Size(width, height), 5, Colors.Black, null, null));

		exception.ParamName.Should().Be("desiredSize");
	}

	[Theory]
	[InlineData(double.NaN, 10)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity)]
	public void ImageLineOptionsRejectADesiredSizeWithANaNSideOrTwoInfiniteSides(double width, double height)
	{
		IList<IDrawingLine> lines = [new DrawingLine()];

		Assert.Throws<ArgumentException>(() => ImageLineOptions.JustLines(lines, new Size(width, height), null)).ParamName.Should().Be("desiredSize");
		Assert.Throws<ArgumentException>(() => ImageLineOptions.FullCanvas(lines, new Size(width, height), null, new Size(100, 100))).ParamName.Should().Be("desiredSize");
	}

	// One infinite side fits the image to the other side; a negative side, -Infinity included, gives the minimum image of one pixel
	[Theory]
	[InlineData(0, 0)]
	[InlineData(-5, 10)]
	[InlineData(0.2, 100)]
	[InlineData(double.MaxValue, double.MinValue)]
	[InlineData(double.PositiveInfinity, 10)]
	[InlineData(10, double.PositiveInfinity)]
	[InlineData(10, double.NegativeInfinity)]
	[InlineData(double.NegativeInfinity, double.NegativeInfinity)]
	[InlineData(double.PositiveInfinity, double.NegativeInfinity)]
	public void DesiredSizesThatTheImageCanBeFittedIntoAreKept(double width, double height)
	{
		var desiredSize = new Size(width, height);

		new ImagePointOptions([], desiredSize, 5, Colors.Black, null, null).DesiredSize.Should().Be(desiredSize);
		ImageLineOptions.JustLines([], desiredSize, null).DesiredSize.Should().Be(desiredSize);
		ImageLineOptions.FullCanvas([], desiredSize, null, new Size(100, 100)).DesiredSize.Should().Be(desiredSize);
	}

	// The canvas size is the size of the image that is drawn before the resize: a NaN side or a side of positive infinity cannot be drawn
	[Theory]
	[InlineData(double.NaN, 50)]
	[InlineData(100, double.NaN)]
	[InlineData(double.PositiveInfinity, 50)]
	[InlineData(100, double.PositiveInfinity)]
	public void ImagePointOptionsAndFullCanvasRejectACanvasSizeWithANaNSideOrASideOfPositiveInfinity(double width, double height)
	{
		IList<IDrawingLine> lines = [new DrawingLine()];
		var canvasSize = new Size(width, height);

		Assert.Throws<ArgumentException>(() => new ImagePointOptions([new PointF(1, 1)], new Size(100, 50), 5, Colors.Black, null, canvasSize)).ParamName.Should().Be("canvasSize");
		Assert.Throws<ArgumentException>(() => ImageLineOptions.FullCanvas(lines, new Size(100, 50), null, canvasSize)).ParamName.Should().Be("canvasSize");
	}

	// A canvas with a side below one pixel, -Infinity included, gives Stream.Null instead of an image (DrawingViewServiceTests), as before the validation
	[Theory]
	[InlineData(100, 50)]
	[InlineData(0, 0)]
	[InlineData(-5, 10)]
	[InlineData(double.NegativeInfinity, 50)]
	[InlineData(100, double.NegativeInfinity)]
	public void CanvasSizesThatCanBeDrawnOrAreBelowOnePixelAreKept(double width, double height)
	{
		var canvasSize = new Size(width, height);

		new ImagePointOptions([], new Size(100, 50), 5, Colors.Black, null, canvasSize).CanvasSize.Should().Be(canvasSize);
		ImageLineOptions.FullCanvas([], new Size(100, 50), null, canvasSize).CanvasSize.Should().Be(canvasSize);
	}

	[Fact]
	public void NoCanvasSizeIsKept()
	{
		new ImagePointOptions([], new Size(100, 50), 5, Colors.Black, null, null).CanvasSize.Should().BeNull();
		ImageLineOptions.FullCanvas([], new Size(100, 50), null, null).CanvasSize.Should().BeNull();
		ImageLineOptions.JustLines([], new Size(100, 50), null).CanvasSize.Should().BeNull();
	}
}