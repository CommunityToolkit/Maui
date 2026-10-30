using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DrawingView)]
public partial class DrawingViewServiceTests
{
	static readonly Color red = Colors.Red;
	static readonly Color blue = Colors.Blue;
	static readonly Color white = Colors.White;

	// The bounding box of these points padded by the line width: x 10..50, y 10..30 plus 4 on every side = 48 x 28 pixels.
	static readonly (float X, float Y)[] lShape = [(10, 10), (50, 10), (50, 30)];

	[Fact]
	public async Task PointsWithoutACanvasProduceAPngCoveringTheirBoundingBox()
	{
		var options = new ImagePointOptions(ToPoints(lShape), new Size(48, 28), 4, red, new SolidPaint(white), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		AssertPng(stream);
		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 48, 28);
		// image coordinates = point - (minX, minY): (10,10) -> (4,4), (50,10) -> (44,4), (50,30) -> (44,24)
		DrawingPixelAssert.NotColorAtUnits(image, 24, 4, white, because: "the horizontal segment is drawn");
		DrawingPixelAssert.NotColorAtUnits(image, 44, 14, white, because: "the vertical segment is drawn");
		DrawingPixelAssert.ColorAtUnits(image, 10, 20, white, because: "away from the polyline the background shows");
		DrawingPixelAssert.ColorAtUnits(image, 24, 14, white);
	}

	[Fact]
	public async Task PointsWithACanvasSizeProduceAPngOfTheCanvasSizeWithPointsInCanvasCoordinates()
	{
		var options = new ImagePointOptions(ToPoints((20, 20), (80, 20)), new Size(100, 50), 6, red, new SolidPaint(white), new Size(100, 50));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 100, 50);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white, because: "the segment is drawn at its canvas coordinates");
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, white);
		DrawingPixelAssert.ColorAtUnits(image, 5, 5, white);
	}

	[Fact]
	public async Task PointsAreStrokedWithTheStrokeColor()
	{
		var options = new ImagePointOptions(ToPoints((10, 20), (90, 20)), new Size(100, 40), 8, blue, new SolidPaint(white), new Size(100, 40));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white, because: "the segment is drawn, whatever its color");
		DrawingPixelAssert.ColorAtUnits(image, 50, 34, white);
		AssertStrokeColor(image, 50, 20, blue);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task StrokesHaveRoundCapsAndRoundJoins(bool useLines)
	{
		// a horizontal 20 wide line: its round caps are half circles of radius 10 around (120,50) and (170,50)
		// a 14 wide "V" with its corner at (50,25): a round join reaches 7 units beyond the corner, a miter join 17, a bevel join 3
		DrawingPixelImage capImage, joinImage;
		var canvas = new Size(200, 100);
		if (useLines)
		{
			var lines = new List<IDrawingLine> { CreateLine(red, 20, (120, 50), (170, 50)), CreateLine(red, 14, (30, 70), (50, 25), (70, 70)) };
			using var stream = await DrawingViewService.GetImageStream(ImageLineOptions.FullCanvas(lines, canvas, new SolidPaint(white), canvas));
			capImage = joinImage = await DecodeAsync(stream);
		}
		else
		{
			using var capStream = await DrawingViewService.GetImageStream(new ImagePointOptions(ToPoints((120, 50), (170, 50)), canvas, 20, red, new SolidPaint(white), canvas));
			using var joinStream = await DrawingViewService.GetImageStream(new ImagePointOptions(ToPoints((30, 70), (50, 25), (70, 70)), canvas, 14, red, new SolidPaint(white), canvas));
			capImage = await DecodeAsync(capStream);
			joinImage = await DecodeAsync(joinStream);
		}

		DrawingPixelAssert.NotColorAtUnits(capImage, 113, 50, white, because: "a butt cap would end at x = 120");
		DrawingPixelAssert.NotColorAtUnits(capImage, 177, 50, white, because: "a butt cap would end at x = 170");
		DrawingPixelAssert.ColorAtUnits(capImage, 112, 42, white, because: "a square cap would cover the corner of the cap, which is outside the round cap");
		DrawingPixelAssert.ColorAtUnits(capImage, 178, 58, white, because: "a square cap would cover the corner of the cap, which is outside the round cap");
		DrawingPixelAssert.NotColorAtUnits(joinImage, 50, 19.5, white, because: "a bevel join would stop 3 units beyond the corner");
		DrawingPixelAssert.ColorAtUnits(joinImage, 50, 12, white, because: "a miter join would reach 17 units beyond the corner");
		AssertStrokeColor(capImage, 113, 50, red);
		AssertStrokeColor(capImage, 177, 50, red);
		AssertStrokeColor(joinImage, 50, 19.5, red);
	}

	[Theory]
	[InlineData(0.5, 10)]
	[InlineData(10, 0.5)]
	[InlineData(0, 0)]
	[InlineData(double.NegativeInfinity, 10)]
	public async Task PointsWithACanvasSmallerThanOnePixelReturnNullStream(double canvasWidth, double canvasHeight)
	{
		var options = new ImagePointOptions(ToPoints((1, 1), (5, 5)), new Size(10, 10), 2, red, null, new Size(canvasWidth, canvasHeight));

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	[Theory]
	[InlineData(0.5, 10)]
	[InlineData(10, 0.5)]
	[InlineData(0, 0)]
	[InlineData(10, double.NegativeInfinity)]
	public async Task LinesWithACanvasSmallerThanOnePixelReturnNullStream(double canvasWidth, double canvasHeight)
	{
		var lines = new List<IDrawingLine> { CreateLine(red, 2, (1, 1), (5, 5)) };
		var options = ImageLineOptions.FullCanvas(lines, new Size(10, 10), null, new Size(canvasWidth, canvasHeight));

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	[Fact]
	public async Task SinglePointWithZeroLineWidthReturnsNullStream()
	{
		var options = new ImagePointOptions(ToPoints((5, 5)), new Size(10, 10), 0, red, null, null);

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	// Apple used to fail in points.Min(). The DrawingLine unit tests that expect Stream.Null only run against the net stub, so they do not settle the contract.
	[Fact]
	public async Task EmptyPointsReturnNullStream()
	{
		var options = new ImagePointOptions([], new Size(10, 10), 4, red, null, null);

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	// The image is 12x12: the point padded by the 6 unit line width on both sides.
	[Fact]
	public async Task SinglePointProducesAnImageOfTheLineWidthPadding()
	{
		var options = new ImagePointOptions(ToPoints((5, 5)), new Size(12, 12), 6, red, new SolidPaint(white), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 12, 12);
		// the point (5,5) is at (6,6) of the image
		DrawingPixelAssert.NotColorAtUnits(image, 6, 6, white, because: "the single point is drawn as a dot of the line width");
	}

	[Fact]
	public async Task SinglePointLineAppearsInTheImageAsADot()
	{
		var lines = new List<IDrawingLine> { CreateLine(red, 10, (50, 25)) };
		var options = ImageLineOptions.FullCanvas(lines, new Size(100, 50), new SolidPaint(white), new Size(100, 50));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 100, 50);
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, white, because: "the dot has a diameter of 10 units");
		DrawingPixelAssert.NotColorAtUnits(image, 50, 25, white, because: "the single point is drawn as a dot of the line width");
	}

	[Fact]
	public async Task LinesProduceAPngPaddedByTheLargestLineWidthWithEveryLineDrawn()
	{
		var lines = new List<IDrawingLine>
		{
			CreateLine(red, 4, (10, 10), (50, 10)),
			CreateLine(blue, 8, (10, 30), (50, 30))
		};
		var options = ImageLineOptions.JustLines(lines, new Size(56, 36), new SolidPaint(white));

		using var stream = await DrawingViewService.GetImageStream(options);

		AssertPng(stream);
		var image = await DecodeAsync(stream);
		// minX = 10 - 8 = 2, minY = 10 - 8 = 2; width = 50 - 2 + 8 = 56, height = 30 - 2 + 8 = 36
		DrawingPixelAssert.SizeIs(image, 56, 36);
		DrawingPixelAssert.NotColorAtUnits(image, 28, 8, white, because: "line A is drawn");
		DrawingPixelAssert.NotColorAtUnits(image, 28, 28, white, because: "line B is drawn");
		DrawingPixelAssert.NotColorAtUnits(image, 28, 31, white, because: "line B is 8 units wide: covers y 24..32");
		DrawingPixelAssert.ColorAtUnits(image, 28, 12, white, because: "line A is 4 units wide: covers y 6..10");
		DrawingPixelAssert.ColorAtUnits(image, 28, 18, white);
	}

	[Fact]
	public async Task LinesAreStrokedWithTheirOwnColors()
	{
		var lines = new List<IDrawingLine>
		{
			CreateLine(red, 6, (10, 10), (90, 10)),
			CreateLine(blue, 6, (10, 30), (90, 30))
		};
		var options = ImageLineOptions.FullCanvas(lines, new Size(100, 40), new SolidPaint(white), new Size(100, 40));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 10, white, because: "line A is drawn, whatever its color");
		DrawingPixelAssert.NotColorAtUnits(image, 50, 30, white, because: "line B is drawn, whatever its color");
		DrawingPixelAssert.ColorAtUnits(image, 50, 20, white);
		AssertStrokeColor(image, 50, 10, red);
		AssertStrokeColor(image, 50, 30, blue);
	}

	[Fact]
	public async Task LinesWithACanvasSizeProduceAPngOfTheCanvasSize()
	{
		var lines = new List<IDrawingLine> { CreateLine(red, 6, (20, 20), (80, 20)) };
		var options = ImageLineOptions.FullCanvas(lines, new Size(100, 50), new SolidPaint(white), new Size(100, 50));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 100, 50);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white);
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, white);
	}

	// DrawingView.GetImageStream on an empty drawing: Android used to fail in Max() over no line widths, iOS and Mac Catalyst threw "Unable to generate image. No Lines Found"
	[Fact]
	public async Task NoLinesReturnNullStream()
	{
		var options = ImageLineOptions.JustLines([], new Size(10, 10), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	[Fact]
	public async Task NoLinesWithACanvasSizeReturnNullStream()
	{
		var options = ImageLineOptions.FullCanvas([], new Size(100, 50), new SolidPaint(white), new Size(100, 50));

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	[Fact]
	public async Task LineWithoutPointsReturnsNullStream()
	{
		var options = ImageLineOptions.JustLines([new DrawingLine()], new Size(10, 10), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		Assert.Same(Stream.Null, stream);
	}

	[Fact]
	public async Task LineWithoutPointsNextToAValidLineIsSkipped()
	{
		var lines = new List<IDrawingLine>
		{
			new DrawingLine(),
			CreateLine(red, 6, (10, 20), (90, 20))
		};
		var options = ImageLineOptions.FullCanvas(lines, new Size(100, 40), new SolidPaint(white), new Size(100, 40));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white, because: "the valid line is drawn");
	}

	[Fact]
	public async Task PointsWithNegativeCoordinatesProduceTheSameImageAsTheSameShapeAtPositiveCoordinates()
	{
		var positive = new ImagePointOptions(ToPoints(lShape), new Size(48, 28), 4, red, new SolidPaint(white), null);
		var negative = new ImagePointOptions(ToPoints((-90, -50), (-50, -50), (-50, -30)), new Size(48, 28), 4, red, new SolidPaint(white), null);

		using var positiveStream = await DrawingViewService.GetImageStream(positive);
		using var negativeStream = await DrawingViewService.GetImageStream(negative);

		var expected = await DecodeAsync(positiveStream);
		var actual = await DecodeAsync(negativeStream);
		DrawingPixelAssert.NotColorAtUnits(actual, 24, 4, white, because: "the shape is drawn, so the comparison below is not between two blank images");
		DrawingPixelAssert.SameImage(expected, actual, because: "the shape is the same, only shifted by (-100,-60)");
	}

	[Fact]
	public async Task ImageIsTheSameWhetherRequestedOnTheUiThreadOrOnAWorkerThread()
	{
		var options = new ImagePointOptions(ToPoints(lShape), new Size(48, 28), 4, red, new SolidPaint(white), null);

		using var workerStream = await Task.Run(async () => await DrawingViewService.GetImageStream(options));
		using var uiStream = await TestDispatcher.Current.DispatchAsync(async () => await DrawingViewService.GetImageStream(options));

		var workerImage = await DecodeAsync(workerStream);
		var uiImage = await DecodeAsync(uiStream);
		DrawingPixelAssert.NotColorAtUnits(uiImage, 24, 4, white, because: "the shape is drawn");
		DrawingPixelAssert.SameImage(workerImage, uiImage);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SolidPaintBackgroundFillsTheImage(bool useLines)
	{
		var green = Colors.Green;

		using var stream = await GetStreamAsync(useLines, new SolidPaint(green));

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 3, 3, green);
		DrawingPixelAssert.ColorAtUnits(image, 97, 47, green);
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, green);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SolidPaintWithoutAColorFillsTheImageWithTheDefaultLightGray(bool useLines)
	{
		using var stream = await GetStreamAsync(useLines, new SolidPaint());

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 3, 3, Colors.LightGray);
		DrawingPixelAssert.ColorAtUnits(image, 97, 47, Colors.LightGray);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task NullBackgroundFillsTheImageWithTheDefaultLightGray(bool useLines)
	{
		using var stream = await GetStreamAsync(useLines, null);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 3, 3, Colors.LightGray);
		DrawingPixelAssert.ColorAtUnits(image, 97, 47, Colors.LightGray);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task UnsupportedPaintFillsTheImageWithTheDefaultLightGray(bool useLines)
	{
		using var stream = await GetStreamAsync(useLines, new ImagePaint());

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 3, 3, Colors.LightGray);
		DrawingPixelAssert.ColorAtUnits(image, 97, 47, Colors.LightGray);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LinearGradientBackgroundGoesFromTheStartPointToTheEndPointThroughItsStopOffsets(bool useLines)
	{
		var paint = new LinearGradientPaint(
			[new PaintGradientStop(0, red), new PaintGradientStop(0.25f, white), new PaintGradientStop(1, blue)],
			new Point(0, 0.5),
			new Point(1, 0.5));

		using var stream = await GetStreamAsync(useLines, paint);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 1, 5, red, tolerance: 80, because: "the start of the gradient");
		DrawingPixelAssert.ColorAtUnits(image, 25, 5, white, tolerance: 40, because: "the white stop is at a quarter of the width");
		DrawingPixelAssert.ColorAtUnits(image, 50, 5, Color.FromRgb(170, 170, 255), tolerance: 40, because: "a third of the way from the white stop to the blue stop");
		DrawingPixelAssert.ColorAtUnits(image, 98, 5, blue, tolerance: 80, because: "the end of the gradient");
	}

	// This matches gradients everywhere else in MAUI.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task LinearGradientBackgroundExtendsItsEndColorBeyondTheEndPoint(bool useLines)
	{
		var paint = new LinearGradientPaint([new PaintGradientStop(0, red), new PaintGradientStop(1, blue)], new Point(0, 0.5), new Point(0.5, 0.5));

		using var stream = await GetStreamAsync(useLines, paint);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 1, 5, red, tolerance: 80, because: "the start of the gradient");
		DrawingPixelAssert.ColorAtUnits(image, 90, 5, blue, tolerance: 80, because: "beyond the end point (x = 50) the end color continues (Apple converts gradient colors between color spaces: pure blue comes back as about #0432FF)");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RadialGradientBackgroundGoesFromTheCenterOutwards(bool useLines)
	{
		var paint = new RadialGradientPaint(new Point(0.5, 0.5), 0.5) { GradientStops = [new PaintGradientStop(0, red), new PaintGradientStop(1, blue)] };

		using var stream = await GetStreamAsync(useLines, paint);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 50, 25, red, tolerance: 50);
		var corner = image.AtUnits(2, 2);
		Assert.True(corner.B > corner.R + 50, $"The corner of a red to blue radial gradient should be bluer than red, but is {corner}.");
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RadialGradientBackgroundUsesThePaintCenterAndRadius(bool useLines)
	{
		var paint = new RadialGradientPaint(new Point(0.25, 0.5), 0.25) { GradientStops = [new PaintGradientStop(0, red), new PaintGradientStop(1, blue)] };

		using var stream = await GetStreamAsync(useLines, paint);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.ColorAtUnits(image, 25, 25, red, tolerance: 50, because: "the center of the paint is at (0.25, 0.5) of the 100x50 image");
		DrawingPixelAssert.ColorAtUnits(image, 90, 25, blue, tolerance: 50, because: "that is far outside the radius, so the end color is used");
	}

	// MAUI's canvas, which draws the DrawingView on every platform, makes the radial gradient a circle whose radius is relative to the larger side of the image,
	// also when that is the height (Android used the width, Windows drew an ellipse).
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task RadialGradientRadiusIsRelativeToTheLargerSideOfAPortraitImage(bool useLines)
	{
		var paint = new RadialGradientPaint(new Point(0.5, 0.5), 0.5) { GradientStops = [new PaintGradientStop(0, red), new PaintGradientStop(1, blue)] };
		var size = new Size(50, 100);

		using var stream = useLines
			? await DrawingViewService.GetImageStream(ImageLineOptions.FullCanvas([CreateLine(red, 4, (10, 10), (40, 10))], size, paint, size))
			: await DrawingViewService.GetImageStream(new ImagePointOptions(ToPoints((10, 10), (40, 10)), size, 4, red, paint, size));

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 50, 100);
		DrawingPixelAssert.ColorAtUnits(image, 25, 50, red, tolerance: 50, because: "the center of the paint is the center of the 50x100 image");
		DrawingPixelAssert.ColorAtUnits(image, 25, 74, Color.FromRgb(133, 0, 122), tolerance: 50, because: "the radius is half the height (50 units), so 24 units below the center is about halfway from red to blue");
		DrawingPixelAssert.ColorAtUnits(image, 2, 50, Color.FromRgb(138, 0, 117), tolerance: 50, because: "the gradient is a circle, so 23 units left of the center is about halfway too");
	}

	// Android's shaders reject a gradient without stops, with one stop or with a radius of 0, and Apple's CGGradient rejects one without stops,
	// yet every platform must produce an image with the colors that the other platforms give them (a radius of 0 spans the diagonal of the image, as in MAUI's canvas).
	// Apple converts gradient colors between color spaces (pure red comes back as about #FF2600), hence the tolerance.
	[Theory]
	[InlineData("linear-1-stop", false)]
	[InlineData("radial-1-stop", false)]
	[InlineData("radial-radius-0", false)]
	[InlineData("linear-no-stops", false)]
	[InlineData("linear-no-stops", true)]
	[InlineData("radial-no-stops", false)]
	[InlineData("radial-no-stops", true)]
	public async Task DegenerateGradientBackgroundsStillProduceAnImage(string kind, bool useLines)
	{
		var paint = DrawingViewTestPlatform.CreateDegenerateGradient(kind);

		using var stream = await GetStreamAsync(useLines, paint);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 100, 50);
		if (kind is "linear-no-stops" or "radial-no-stops")
		{
			foreach (var (x, y) in new[] { (50.0, 25.0), (2.0, 2.0), (97.0, 47.0) })
			{
				DrawingPixelAssert.ColorAtUnits(image, x, y, DrawingViewDefaults.BackgroundColor, tolerance: 20, because: "a gradient without stops is filled with the default background");
			}
		}
		else if (kind is "radial-radius-0")
		{
			DrawingPixelAssert.ColorAtUnits(image, 50, 25, red, tolerance: 50, because: "the center has the start color");
			DrawingPixelAssert.ColorAtUnits(image, 2, 2, Color.FromRgb(133, 0, 121), tolerance: 50, because: "the radius spans the diagonal of the image (112 units), so the corner, 53 units from the center, is about halfway from red to blue");
		}
		else
		{
			foreach (var (x, y) in new[] { (50.0, 25.0), (2.0, 2.0), (97.0, 2.0), (2.0, 47.0), (97.0, 47.0) })
			{
				DrawingPixelAssert.ColorAtUnits(image, x, y, red, tolerance: 50, because: "a gradient with a single stop fills the image with the color of that stop");
			}
		}
	}

	// Without validation the image is resized by Math.Min(desired width / width, desired height / height), which is then NaN or infinite, and that fails inside the platform
	// image APIs (a NaN side: Java.Lang.IllegalArgumentException from Bitmap.CreateScaledBitmap on Android, UIGraphics.BeginImageContext aborts the process on iOS and Mac Catalyst).
	// The options are built inside the recorded calls, so validation in the options constructors and validation in DrawingViewService both pass.
	[Theory]
	[InlineData(double.NaN, 50)]
	[InlineData(double.PositiveInfinity, double.PositiveInfinity)]
	public async Task DesiredSizeWithANaNSideOrTwoInfiniteSidesIsRejectedWithAnArgumentException(double desiredWidth, double desiredHeight)
	{
		if (DrawingViewTestPlatform.IsApple)
		{
			// Such a size that reaches GetMaximumUIImage aborts the process, so the real call only runs once a probe without points (which never gets to
			// the resize) shows that the size is validated first; a probe that is not rejected fails the test without making the aborting call.
			var probe = await Record.ExceptionAsync(async () =>
			{
				using var probeStream = await DrawingViewService.GetImageStream(new ImagePointOptions([], new Size(desiredWidth, desiredHeight), 4, red, null, null));
			});
			Assert.IsType<ArgumentException>(probe, exactMatch: false);
		}

		var exception = await Record.ExceptionAsync(async () =>
		{
			var options = new ImagePointOptions(ToPoints(lShape), new Size(desiredWidth, desiredHeight), 4, red, new SolidPaint(white), null);
			using var stream = await DrawingViewService.GetImageStream(options);
		});

		Assert.IsType<ArgumentException>(exception, exactMatch: false);
	}

	// The canvas size is the size of the image that is drawn, so without validation it fails inside the platform image APIs (a NaN side: Java.Lang.IllegalArgumentException from
	// Bitmap.CreateBitmap on Android, UIGraphics.BeginImageContextWithOptions aborts the process on iOS and Mac Catalyst).
	// The options are built inside the recorded calls, so validation in the options constructors and validation in DrawingViewService both pass.
	[Theory]
	[InlineData(false, double.NaN, 50)]
	[InlineData(true, double.NaN, 50)]
	[InlineData(false, 100, double.PositiveInfinity)]
	[InlineData(true, 100, double.PositiveInfinity)]
	public async Task CanvasSizeWithANaNSideOrASideOfPositiveInfinityIsRejectedWithAnArgumentException(bool useLines, double canvasWidth, double canvasHeight)
	{
		var canvasSize = new Size(canvasWidth, canvasHeight);
		if (DrawingViewTestPlatform.IsApple)
		{
			// Such a canvas size that reaches GetUIImage aborts the process, so the real call only runs once a probe without points (GetUIImage returns before
			// it creates the image) shows that the size is validated first; a probe that is not rejected fails the test without making the aborting call.
			var probe = await Record.ExceptionAsync(async () =>
			{
				using var probeStream = useLines
					? await DrawingViewService.GetImageStream(ImageLineOptions.FullCanvas([new DrawingLine()], new Size(100, 50), null, canvasSize))
					: await DrawingViewService.GetImageStream(new ImagePointOptions([], new Size(100, 50), 4, red, null, canvasSize));
			});
			Assert.IsType<ArgumentException>(probe, exactMatch: false);
		}

		var exception = await Record.ExceptionAsync(async () =>
		{
			using var stream = useLines
				? await DrawingViewService.GetImageStream(ImageLineOptions.FullCanvas([CreateLine(red, 4, (10, 20), (90, 20))], new Size(100, 50), new SolidPaint(white), canvasSize))
				: await DrawingViewService.GetImageStream(new ImagePointOptions(ToPoints((10, 20), (90, 20)), new Size(100, 50), 4, red, new SolidPaint(white), canvasSize));
		});

		Assert.IsType<ArgumentException>(exception, exactMatch: false);
	}

	// Windows used to keep the natural size. An infinite side of the desired size fits the image to the other side instead of being rejected.
	[Theory]
	[InlineData(96, 200, 96, 56)]
	[InlineData(24, 100, 24, 14)]
	[InlineData(96, 56, 96, 56)]
	[InlineData(480, 280, 480, 280)]
	[InlineData(double.PositiveInfinity, 14, 24, 14)]
	[InlineData(24, double.PositiveInfinity, 24, 14)]
	public async Task PointsAreScaledProportionallyToFitTheDesiredSize(double desiredWidth, double desiredHeight, int expectedWidth, int expectedHeight)
	{
		var options = new ImagePointOptions(ToPoints(lShape), new Size(desiredWidth, desiredHeight), 4, red, new SolidPaint(white), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		// natural size: 48 x 28 (see lShape)
		DrawingPixelAssert.SizeIs(image, expectedWidth, expectedHeight);
	}

	[Theory]
	[InlineData(112, 200, 112, 72)]
	[InlineData(28, 100, 28, 18)]
	[InlineData(double.PositiveInfinity, 18, 28, 18)]
	public async Task LinesAreScaledProportionallyToFitTheDesiredSize(double desiredWidth, double desiredHeight, int expectedWidth, int expectedHeight)
	{
		var lines = new List<IDrawingLine> { CreateLine(red, 8, (10, 10), (50, 10)), CreateLine(blue, 8, (10, 30), (50, 30)) };
		var options = ImageLineOptions.JustLines(lines, new Size(desiredWidth, desiredHeight), new SolidPaint(white));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		// natural size: 56 x 36 (x 10..50 and y 10..30 padded by 8)
		DrawingPixelAssert.SizeIs(image, expectedWidth, expectedHeight);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(-5, 10)]
	[InlineData(0.2, 100)]
	[InlineData(double.NegativeInfinity, 10)]
	public async Task DesiredSizeBelowOnePixelProducesAOnePixelImage(double desiredWidth, double desiredHeight)
	{
		var options = new ImagePointOptions(ToPoints(lShape), new Size(desiredWidth, desiredHeight), 4, red, new SolidPaint(white), null);

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 1, 1);
	}

	[Fact]
	public async Task PointsWithACanvasSizeAreScaledToTheDesiredSize()
	{
		var options = new ImagePointOptions(ToPoints((20, 20), (80, 20)), new Size(200, 100), 6, red, new SolidPaint(white), new Size(100, 50));

		using var stream = await DrawingViewService.GetImageStream(options);

		var image = await DecodeAsync(stream);
		// natural size: the 100 x 50 canvas
		DrawingPixelAssert.SizeIs(image, 200, 100);
	}

	// Windows used to see the token only in SaveAsync(...).AsTask(token), whose outcome depends on timing.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AlreadyCancelledTokenThrowsOperationCanceledException(bool useLines)
	{
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var exception = await Record.ExceptionAsync(async () =>
		{
			using var stream = await GetStreamAsync(useLines, null, cancellation.Token);
		});

		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AlreadyCancelledTokenThrowsEvenWhenThereIsNothingToDraw(bool useLines)
	{
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		var exception = await Record.ExceptionAsync(async () =>
		{
			using var stream = useLines
				? await DrawingViewService.GetImageStream(ImageLineOptions.JustLines([new DrawingLine()], new Size(10, 10), null), cancellation.Token)
				: await DrawingViewService.GetImageStream(new ImagePointOptions([], new Size(10, 10), 4, red, null, null), cancellation.Token);
		});

		Assert.IsType<OperationCanceledException>(exception, exactMatch: false);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task TokenThatIsNotCancelledProducesTheImage(bool useLines)
	{
		using var cancellation = new CancellationTokenSource();

		using var stream = await GetStreamAsync(useLines, null, cancellation.Token);

		Assert.NotSame(Stream.Null, stream);
		AssertPng(stream);
	}

	[Fact]
	public async Task DrawingLineGetImageStreamRendersItsPointsWidthAndBackground()
	{
		var line = new DrawingLine { LineColor = red, LineWidth = 6, Points = new ObservableCollection<PointF>(ToPoints((20, 20), (80, 20))) };

		using var stream = await line.GetImageStream(100, 50, new SolidPaint(white), new Size(100, 50));

		var image = await DecodeAsync(stream);
		DrawingPixelAssert.SizeIs(image, 100, 50);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white);
		DrawingPixelAssert.NotColorAtUnits(image, 50, 22, white, because: "6 units wide: covers y 17..23");
		DrawingPixelAssert.ColorAtUnits(image, 50, 27, white, because: "6 units wide: covers y 17..23");
		DrawingPixelAssert.ColorAtUnits(image, 50, 40, white);
		AssertStrokeColor(image, 50, 20, red);
	}

	Task<Stream> GetStreamAsync(bool useLines, Paint? background, CancellationToken token = default)
	{
		// The two branches must describe the same 100x50 canvas with one line along y = 20, as the tests expect the same pixels from both.
		if (useLines)
		{
			var lines = new List<IDrawingLine> { CreateLine(red, 4, (10, 20), (90, 20)) };
			return DrawingViewService.GetImageStream(ImageLineOptions.FullCanvas(lines, new Size(100, 50), background, new Size(100, 50)), token).AsTask();
		}

		return DrawingViewService.GetImageStream(new ImagePointOptions(ToPoints((10, 20), (90, 20)), new Size(100, 50), 4, red, background, new Size(100, 50)), token).AsTask();
	}

	static List<PointF> ToPoints(params (float X, float Y)[] points) => points.Select(point => new PointF(point.X, point.Y)).ToList();

	static DrawingLine CreateLine(Color color, float width, params (float X, float Y)[] points) => new()
	{
		LineColor = color,
		LineWidth = width,
		ShouldSmoothPathWhenDrawn = false,
		Points = new ObservableCollection<PointF>(ToPoints(points))
	};

	static void AssertPng(Stream stream)
	{
		Assert.True(stream.CanRead);
		Assert.Equal(0, stream.Position);
		var signature = new byte[8];
		var read = stream.Read(signature, 0, signature.Length);
		stream.Position = 0;
		Assert.Equal(8, read);
		Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, signature);
	}

	static async Task<DrawingPixelImage> DecodeAsync(Stream stream)
	{
		Assert.NotSame(Stream.Null, stream);
		stream.Position = 0;
		return await DrawingViewTestPlatform.DecodePngAsync(stream);
	}

	static void AssertStrokeColor(DrawingPixelImage image, double x, double y, Color expected) =>
		DrawingPixelAssert.ColorAtUnits(image, x, y, expected, because: "the stroke color is applied");
}