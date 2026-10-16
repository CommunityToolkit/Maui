using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Maui.Core.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Tests of the platform view (MauiDrawingView) on its own: the platform's touch input, the gesture state machine in
// MauiDrawingView.shared.cs, and what it renders. The view is created directly (no handler) and hosted in the runner's window.
// Every test reads native state: events raised by real platform input, the Lines collection, and the pixels the view draws.
[Category(TestCategory.DrawingView)]
[Collection(TestCollections.DisableParallelization)]
public partial class MauiDrawingViewTests : CommunityToolkitHandlerTestBase
{
	static readonly Color red = Colors.Red;
	static readonly Color blue = Colors.Blue;
	static readonly Color white = Colors.White;

	// Catches: OnStart stops raising DrawingStarted or reports a different point
	[Fact]
	public Task TouchDownRaisesDrawingStartedWithTheTouchPoint() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchDown(view, 30, 40);

		AssertPoint(30, 40, Assert.Single(recorder.Started));
		Assert.Empty(recorder.Points);
		Assert.Empty(recorder.Completed);
		Assert.Equal(0, recorder.Cancelled);
	});

	// Catches: OnMoving stops raising Drawing, reports the wrong point or drops points
	[Fact]
	public Task TouchMovesRaiseDrawingForEachPointInOrder() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, 20, 20);
		DrawingViewTestPlatform.TouchMove(view, 30, 25);
		DrawingViewTestPlatform.TouchMove(view, 40, 60);

		Assert.Equal(3, recorder.Points.Count);
		AssertPoint(20, 20, recorder.Points[0]);
		AssertPoint(30, 25, recorder.Points[1]);
		AssertPoint(40, 60, recorder.Points[2]);
		Assert.Empty(recorder.Completed);
	});

	// Catches: OnMoving no longer returns when no gesture was started
	[Fact]
	public Task TouchMoveBeforeTouchDownIsIgnored() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchMove(view, 50, 50);
		DrawingViewTestPlatform.TouchMove(view, 60, 60);

		Assert.Empty(recorder.Started);
		Assert.Empty(recorder.Points);
		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);
	});

	// Catches: a Move without a started gesture extends the in-progress path (Android adds the point to the path before OnMoving checks that a gesture is active)
	[Fact]
	public Task MovesOutsideAGestureDoNotDrawAnything() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;

		DrawingViewTestPlatform.TouchMove(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);

		var image = await RenderAsync(view);
		DrawingPixelAssert.DoesNotContainColor(image, red, because: "no gesture was started, so nothing is drawn");
	});

	// Catches: OnFinish does not complete the line, or the line loses its start point or a moved point
	[Fact]
	public Task TouchUpCompletesTheLineWithTheStartAndEveryMovedPoint() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, 30, 20);
		DrawingViewTestPlatform.TouchMove(view, 50, 60);
		DrawingViewTestPlatform.TouchUp(view, 50, 60);

		var line = Assert.Single(recorder.Completed);
		Assert.Equal(3, line.Points.Count);
		AssertPoint(10, 10, line.Points[0]);
		AssertPoint(30, 20, line.Points[1]);
		AssertPoint(50, 60, line.Points[2]);
	});

	// Catches: a default LineColor or LineWidth of the view is not the default of the completed line
	[Fact]
	public Task CompletedLineUsesTheDefaultLineColorAndLineWidth() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.Stroke(view, (10, 10), (50, 60));

		var line = Assert.Single(recorder.Completed);
		Assert.Equal(Colors.Black, line.LineColor);
		Assert.Equal(5f, line.LineWidth);
	});

	// Catches: the completed event is raised before the line is in Lines, or carries another line than the one stored
	[Fact]
	public Task CompletedLineIsStoredInLinesWhenTheEventIsRaised() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.Stroke(view, (10, 10), (50, 60));

		var line = Assert.Single(recorder.Completed);
		var linesWhenRaised = Assert.Single(recorder.LinesWhenCompleted);
		Assert.Same(line, Assert.Single(linesWhenRaised));
	});

	// Catches: the line is cleared (or never stored) when the gesture finishes although ShouldClearOnFinish is off
	[Fact]
	public Task LineStaysInLinesAfterTheGestureFinishesByDefault() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.Stroke(view, (10, 10), (50, 60));

		Assert.Same(Assert.Single(recorder.Completed), Assert.Single(view.Lines));
	});

	// Catches: OnFinish completes a line although no gesture was started
	[Fact]
	public Task TouchUpWithoutTouchDownDoesNotCompleteALine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchUp(view, 20, 20);

		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);
	});

	// Catches: OnCancel keeps the line, forgets the event or does not discard the in-progress line
	[Fact]
	public Task TouchCancelRaisesDrawingCancelledAndDiscardsTheLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchMove(view, 30, 30);
		DrawingViewTestPlatform.TouchCancel(view, 30, 30);

		Assert.Equal(1, recorder.Cancelled);
		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);

		// the cancelled line is gone: a later Up has nothing to complete
		DrawingViewTestPlatform.TouchUp(view, 30, 30);
		Assert.Empty(recorder.Completed);
		Assert.Empty(view.Lines);
	});

	// Catches: OnCancel only raises the event while a gesture is active
	[Fact]
	public Task TouchCancelWithoutTouchDownStillRaisesDrawingCancelled() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		DrawingViewTestPlatform.TouchCancel(view, 20, 20);

		Assert.Equal(1, recorder.Cancelled);
		Assert.Empty(view.Lines);
	});

	// Catches: OnFinish leaves the view in the drawing state
	[Fact]
	public Task TouchMoveAfterTouchUpIsIgnored() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		Stroke(view, (10, 10), (30, 30));
		var pointsBefore = recorder.Points.Count;

		DrawingViewTestPlatform.TouchMove(view, 60, 60);
		DrawingViewTestPlatform.TouchUp(view, 60, 60);

		Assert.Equal(pointsBefore, recorder.Points.Count);
		Assert.Single(recorder.Completed);
		Assert.Single(view.Lines);
		Assert.Equal(2, view.Lines[0].Points.Count);
	});

	// Catches: OnCancel leaves the view in the drawing state
	[Fact]
	public Task TouchMoveAfterTouchCancelIsIgnored() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		DrawingViewTestPlatform.TouchCancel(view, 10, 10);

		DrawingViewTestPlatform.TouchMove(view, 60, 60);

		Assert.Empty(recorder.Points);
	});

	// Catches: the product raises DrawingCancelled with EventArgs.Empty although the event is declared with MauiDrawingStartedEventArgs
	[Fact]
	public Task DrawingCancelledHandlerWithTheDeclaredEventArgsTypeIsInvoked() => WithView(view =>
	{
		var recorder = new TypedCancelRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 10);

		DrawingViewTestPlatform.TouchCancel(view, 10, 10);

		Assert.Equal(1, recorder.Cancelled);
	});

	// Catches: DrawingCancelled reports another point than the start of the cancelled drawing (its args are the declared MauiDrawingStartedEventArgs)
	[Fact]
	public Task DrawingCancelledReportsThePointWhereTheCancelledDrawingStarted() => WithView(view =>
	{
		var recorder = new TypedCancelRecorder(view);
		DrawingViewTestPlatform.TouchDown(view, 10, 20);
		DrawingViewTestPlatform.TouchMove(view, 30, 40);

		DrawingViewTestPlatform.TouchCancel(view, 30, 40);

		AssertPoint(10, 20, Assert.Single(recorder.Points));
	});

	// Catches: a remove accessor does not remove (the weak event manager keeps invoking the handler)
	[Theory]
	[InlineData(nameof(MauiDrawingView.DrawingStarted))]
	[InlineData(nameof(MauiDrawingView.Drawing))]
	[InlineData(nameof(MauiDrawingView.DrawingLineCompleted))]
	[InlineData(nameof(MauiDrawingView.DrawingCancelled))]
	public Task UnsubscribedHandlerIsNoLongerInvoked(string eventName) => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		string[] otherEvents = [nameof(MauiDrawingView.DrawingStarted), nameof(MauiDrawingView.Drawing), nameof(MauiDrawingView.DrawingLineCompleted), nameof(MauiDrawingView.DrawingCancelled)];
		void Gestures()
		{
			Stroke(view, (10, 10), (30, 30));
			DrawingViewTestPlatform.TouchDown(view, 10, 10);
			DrawingViewTestPlatform.TouchCancel(view, 10, 10);
		}

		Gestures();
		var before = otherEvents.ToDictionary(name => name, recorder.Count);
		Assert.All(before.Values, count => Assert.True(count > 0, "Precondition: every event is raised by the gestures."));

		recorder.Unsubscribe(eventName);
		Gestures();

		Assert.Equal(before[eventName], recorder.Count(eventName));
		foreach (var other in otherEvents.Where(name => name != eventName))
		{
			Assert.True(recorder.Count(other) > before[other], $"{other} must still be raised after {eventName} was unsubscribed.");
		}
	});

	// Catches: single-line mode keeps the previous line
	[Fact]
	public Task SingleLineModeKeepsOnlyTheLatestLine() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);

		Stroke(view, (10, 10), (40, 10));
		Stroke(view, (10, 50), (40, 50));

		Assert.Equal(2, recorder.Completed.Count);
		var line = Assert.Single(view.Lines);
		AssertPoint(10, 50, line.Points[0]);
		Assert.Single(recorder.LinesWhenCompleted[1]);
	});

	// Catches: multi-line mode clears the previous line
	[Fact]
	public Task MultiLineModeKeepsEveryLine() => WithView(view =>
	{
		view.IsMultiLineModeEnabled = true;

		Stroke(view, (10, 10), (40, 10));
		Stroke(view, (10, 50), (40, 50));

		Assert.Equal(2, view.Lines.Count);
		AssertPoint(10, 10, view.Lines[0].Points[0]);
		AssertPoint(10, 50, view.Lines[1].Points[0]);
	});

	// Catches: the previous line is cleared when the gesture finishes instead of when the next one starts
	[Fact]
	public Task StartingAnotherLineInSingleLineModeClearsThePreviousLineAtOnce() => WithView(view =>
	{
		Stroke(view, (10, 10), (40, 10));
		Assert.Single(view.Lines);

		DrawingViewTestPlatform.TouchDown(view, 10, 50);

		Assert.Empty(view.Lines);
	});

	// Catches: ShouldClearOnFinish ignored, or the lines are cleared before the completed event
	[Fact]
	public Task ShouldClearOnFinishRemovesTheLinesAfterTheCompletedEvent() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.ShouldClearOnFinish = true;
		var recorder = new GestureRecorder(view);

		Stroke(view, (20, 50), (180, 50));

		var line = Assert.Single(recorder.Completed);
		Assert.Equal(2, line.Points.Count);
		Assert.Single(Assert.Single(recorder.LinesWhenCompleted));
		Assert.Empty(view.Lines);
		var image = await RenderAsync(view);
		DrawingPixelAssert.DoesNotContainColor(image, Colors.Black, because: "Lines and the in-progress path were cleared.");
	});

	// Catches: the line takes the color or width from the view at the end of the gesture instead of the start
	[Fact]
	public Task LineColorAndWidthAreCapturedWhenTheGestureStarts() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		view.LineColor = red;
		view.LineWidth = 7;

		DrawingViewTestPlatform.TouchDown(view, 10, 10);
		view.LineColor = blue;
		view.LineWidth = 9;
		DrawingViewTestPlatform.TouchMove(view, 40, 40);
		DrawingViewTestPlatform.TouchUp(view, 40, 40);

		var line = Assert.Single(recorder.Completed);
		Assert.Equal(red, line.LineColor);
		Assert.Equal(7f, line.LineWidth);
	});

	// Catches: default Paint is not LightGray, or the background is not filled
	[Fact]
	public Task RenderingFillsTheViewWithTheDefaultLightGrayBackground() => WithViewAsync(async view =>
	{
		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, 100, 50, Colors.LightGray);
		DrawingPixelAssert.ColorAtUnits(image, 2, 2, Colors.LightGray);
		DrawingPixelAssert.ColorAtUnits(image, 197, 97, Colors.LightGray);
	});

	// Catches: the Paint setter does not store the value
	[Theory]
	[InlineData(0xFF, 0x00, 0x00)]
	[InlineData(0x00, 0x80, 0x00)]
	[InlineData(0x00, 0x00, 0xFF)]
	public Task SettingPaintChangesTheRenderedBackground(int r, int g, int b) => WithViewAsync(async view =>
	{
		var color = Color.FromRgb(r, g, b);

		view.Paint = new SolidPaint(color);

		var image = await RenderAsync(view);
		DrawingPixelAssert.ColorAtUnits(image, 100, 50, color);
		DrawingPixelAssert.ColorAtUnits(image, 3, 3, color);
	});

	// Catches: gradient paints are not forwarded to the canvas
	[Fact]
	public Task GradientPaintIsRenderedAsTheBackground() => WithViewAsync(async view =>
	{
		view.Paint = new LinearGradientPaint([new PaintGradientStop(0, red), new PaintGradientStop(1, blue)], new Point(0, 0.5), new Point(1, 0.5));

		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, 3, 50, red, tolerance: 60);
		DrawingPixelAssert.ColorAtUnits(image, 197, 50, blue, tolerance: 60);
		DrawingPixelAssert.NotColorAtUnits(image, 100, 50, red, tolerance: 60);
		DrawingPixelAssert.NotColorAtUnits(image, 100, 50, blue, tolerance: 60);
	});

	// Catches: the lines are painted with the background gradient instead of their own color, which hides them. MAUI's Android canvas
	// (Microsoft.Maui.Graphics 10.0.70 and later) strokes a path with the fill paint while that paint is a gradient.
	[Theory]
	[InlineData("linear")]
	[InlineData("radial")]
	public Task StoredLinesAndTheInProgressPathKeepTheirColorAboveAGradientBackground(string kind) => WithViewAsync(async view =>
	{
		view.Paint = CreateWhiteToYellowGradient(kind);
		view.IsMultiLineModeEnabled = true; // otherwise starting the gesture clears the stored line
		view.LineColor = blue;
		view.LineWidth = 10;
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 25), new(180, 25)] });
		DrawingViewTestPlatform.TouchDown(view, 20, 75);
		DrawingViewTestPlatform.TouchMove(view, 180, 75);

		var image = await RenderAsync(view);

		DrawingPixelAssert.SegmentHasColor(image, (20, 25), (180, 25), red, because: $"the stored line has its own color above the {kind} gradient");
		DrawingPixelAssert.SegmentHasColor(image, (20, 75), (180, 75), blue, because: $"the in-progress path has the view's LineColor above the {kind} gradient");
		DrawingPixelAssert.ColorAtUnits(image, 3, 50, white, tolerance: 60, because: "the gradient is still the background");
		DrawingPixelAssert.ColorAtUnits(image, 197, 50, Colors.Yellow, tolerance: 60, because: "the gradient is still the background");
	});

	// Catches: the paths that the DrawAction strokes are painted with the background gradient (see StoredLinesAndTheInProgressPathKeepTheirColorAboveAGradientBackground)
	[Fact]
	public Task DrawActionPathsKeepTheirStrokeColorAboveAGradientBackground() => WithViewAsync(async view =>
	{
		view.Paint = CreateWhiteToYellowGradient("linear");
		view.DrawAction = (canvas, _) =>
		{
			canvas.StrokeColor = red;
			canvas.StrokeSize = 10;
			using var path = new PathF();
			path.MoveTo(20, 50);
			path.LineTo(180, 50);
			canvas.DrawPath(path);
		};

		var image = await RenderAsync(view);

		DrawingPixelAssert.SegmentHasColor(image, (20, 50), (180, 50), red, because: "the DrawAction's path has the DrawAction's stroke color above the gradient");
		DrawingPixelAssert.ColorAtUnits(image, 197, 50, Colors.Yellow, tolerance: 60, because: "the gradient is still the background");
	});

	// Catches: DrawAction is not invoked with the canvas and the view's bounds
	[Fact]
	public Task DrawActionIsInvokedWithTheCanvasAndTheViewBounds()
	{
		var counter = new DrawCounter();

		return WithViewAsync(async view =>
		{
			await RenderAsync(view);

			Assert.NotEmpty(counter.Rects);
			Assert.Contains(counter.Rects, rect => Math.Abs(rect.Width - DrawingViewTestPlatform.DefaultWidth) < 1 && Math.Abs(rect.Height - DrawingViewTestPlatform.DefaultHeight) < 1);
		}, view => view.DrawAction = counter.Draw);
	}

	// Catches: DrawAction is drawn after the lines (covering them) or before the background
	[Fact]
	public Task DrawActionPaintsAboveTheBackgroundAndBelowTheLines() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.DrawAction = (canvas, _) =>
		{
			canvas.FillColor = blue;
			canvas.FillRectangle(20, 20, 60, 40);
		};
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 6, ShouldSmoothPathWhenDrawn = false, Points = [new(10, 40), new(90, 40)] });

		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, 50, 40, red, because: "the stored line is above the DrawAction output");
		DrawingPixelAssert.ColorAtUnits(image, 50, 28, blue, because: "the DrawAction rectangle is above the background");
		DrawingPixelAssert.ColorAtUnits(image, 150, 50, white);
	});

	// Catches: a null DrawAction is dereferenced or the cleared action is still invoked
	[Fact]
	public Task NullDrawActionIsSkippedAndAClearedActionStopsDrawing()
	{
		var counter = new DrawCounter();

		return WithViewAsync(async view =>
		{
			await RenderAsync(view);
			var invoked = counter.Count;
			Assert.True(invoked > 0, "The DrawAction was not invoked while the view was drawn.");

			view.DrawAction = null;
			view.Paint = new SolidPaint(white);
			var image = await RenderAsync(view);

			Assert.Equal(invoked, counter.Count);
			DrawingPixelAssert.ColorAtUnits(image, 100, 50, white);
		}, view => view.DrawAction = counter.Draw);
	}

	// Assigning DrawAction must repaint the view with the new action, as assigning Paint does
	[Fact]
	public Task AssigningDrawActionRedrawsTheView()
	{
		var first = new DrawCounter();
		var second = new DrawCounter();

		return WithViewAsync(async view =>
		{
			await WaitForDrawsToSettleAsync(first);

			view.DrawAction = second.Draw;

			await AssertionExtensions.AssertEventually(() => second.Count > 0, timeout: 3000, message: "The new DrawAction was never invoked: assigning DrawAction did not redraw the view.");
		}, view => view.DrawAction = first.Draw);
	}

	// Catches: stored lines ignore their own color/width or are not redrawn when added
	[Fact]
	public Task StoredLinesAreRenderedWithTheirOwnColorAndWidth() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);

		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 25), new(180, 25)] });
		view.Lines.Add(new MauiDrawingLine { LineColor = blue, LineWidth = 4, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 75), new(180, 75)] });

		var image = await RenderAsync(view);
		DrawingPixelAssert.SegmentHasColor(image, (20, 25), (180, 25), red);
		DrawingPixelAssert.ColorAtUnits(image, 100, 28, red, because: "10 units wide: covers y 20..30");
		DrawingPixelAssert.ColorAtUnits(image, 100, 34, white);
		DrawingPixelAssert.SegmentHasColor(image, (20, 75), (180, 75), blue);
		DrawingPixelAssert.ColorAtUnits(image, 100, 75, blue, because: "4 units wide: covers y 73..77");
		DrawingPixelAssert.ColorAtUnits(image, 100, 80, white);
		DrawingPixelAssert.ColorAtUnits(image, 100, 50, white);
	});

	// A stored line with a single point (what a tap produces) is rendered as a dot of its line width: a zero-length segment with round caps
	[Fact]
	public Task StoredSinglePointLineIsRenderedAsADot() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(100, 50)] });

		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, 100, 50, red, because: "the zero-length segment of a 1-point line is drawn with round caps");
		DrawingPixelAssert.ColorAtUnits(image, 100, 60, white, because: "the dot has the line width (10) as its diameter");
	});

	// Gradient paints that a platform gradient API rejects (one stop, zero radius) are still rendered as the background, without an error.
	// The color is MAUI's business, not the toolkit's: Apple draws the gradient, while MAUI's Android canvas catches the Android exception and falls back to a
	// blended color (white for these paints). The image export of the same paints is DrawingViewServiceTests.DegenerateGradientBackgroundsStillProduceAnImage.
	[Theory]
	[InlineData("linear-1-stop")]
	[InlineData("radial-1-stop")]
	[InlineData("radial-radius-0")]
	public Task DegenerateGradientPaintsAreRenderedAsTheBackground(string kind) => WithViewAsync(async view =>
	{
		view.Paint = DrawingViewTestPlatform.CreateDegenerateGradient(kind);

		var image = await RenderAsync(view);

		var center = image.AtUnits(100, 50);
		Assert.True(center.A is 255, $"The background was not filled with the {kind} paint: found {center} at the center.");
	});

	// Catches: a line without points breaks rendering of the other lines
	[Fact]
	public Task LineWithoutPointsIsSkippedWhenRendering() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);

		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, Points = [] });
		view.Lines.Add(new MauiDrawingLine { LineColor = blue, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 50), new(180, 50)] });

		var image = await RenderAsync(view);
		DrawingPixelAssert.SegmentHasColor(image, (20, 50), (180, 50), blue);
		DrawingPixelAssert.DoesNotContainColor(image, red);
	});

	// Catches: removing lines does not repaint the view
	[Fact]
	public Task ClearingLinesRemovesThemFromTheRenderedView() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 50), new(180, 50)] });
		DrawingPixelAssert.ContainsColor(await RenderAsync(view), red);

		view.Lines.Clear();

		DrawingPixelAssert.DoesNotContainColor(await RenderAsync(view), red);
	});

	// Catches: the in-progress path is not drawn, ignores LineWidth or does not follow moves
	[Fact]
	public Task InProgressPathIsRenderedWithTheViewLineColorAndWidth() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;

		DrawingViewTestPlatform.TouchDown(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);

		var image = await RenderAsync(view);
		DrawingPixelAssert.SegmentHasColor(image, (20, 50), (180, 50), red);
		DrawingPixelAssert.ColorAtUnits(image, 100, 53, red, because: "10 units wide: covers y 45..55");
		DrawingPixelAssert.ColorAtUnits(image, 100, 47, red, because: "10 units wide: covers y 45..55");
		DrawingPixelAssert.ColorAtUnits(image, 100, 59, white);
		DrawingPixelAssert.ColorAtUnits(image, 100, 41, white);
	});

	// Catches: finishing a line leaves the in-progress path (drawn with the current color) on top of the stored line
	[Fact]
	public Task FinishedLineReplacesTheInProgressPath() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;
		DrawingViewTestPlatform.TouchDown(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);

		view.LineColor = blue;
		var duringGesture = await RenderAsync(view);
		DrawingPixelAssert.ColorAtUnits(duringGesture, 100, 50, blue, because: "the in-progress path uses the view's current color");

		DrawingViewTestPlatform.TouchUp(view, 180, 50);

		var afterGesture = await RenderAsync(view);
		DrawingPixelAssert.ColorAtUnits(afterGesture, 100, 50, red, because: "the stored line keeps the color captured at the start");
		DrawingPixelAssert.DoesNotContainColor(afterGesture, blue, because: "the in-progress path was replaced by the stored line");
	});

	// Catches: cancelling leaves the in-progress stroke visible
	[Fact]
	public Task CancelRemovesTheInProgressStroke() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;
		DrawingViewTestPlatform.TouchDown(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);
		DrawingPixelAssert.ContainsColor(await RenderAsync(view), red);

		DrawingViewTestPlatform.TouchCancel(view, 180, 50);

		DrawingPixelAssert.DoesNotContainColor(await RenderAsync(view), red);
	});

	// Catches: ShouldSmoothPathWhenDrawn is ignored (or always/never smoothed)
	[Fact]
	public Task SmoothedLinesFollowTheSplineAndUnsmoothedLinesTheRawPoints() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		var raw = new PointF[] { new(16, 30), new(31, 90), new(49, 10), new(94, 10), new(98, 90), new(147, 20), new(151, 70), new(187, 70) };
		var smoothed = raw.CreateSmoothedPathWithGranularity(5);
		// a point of the raw segment (49,10)-(94,10) that the smoothed curve is far from
		var probe = new PointF(70, 10);
		var distance = DistanceToPolyline(probe, smoothed);
		Assert.True(distance > 4, $"Test data no longer discriminates: the smoothed curve is only {distance} units from the probe.");

		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 2, ShouldSmoothPathWhenDrawn = true, Granularity = 5, Points = new(raw) });
		var smoothedImage = await RenderAsync(view);
		DrawingPixelAssert.NotColorAtUnits(smoothedImage, probe.X, probe.Y, red, because: "the smoothed curve leaves the raw segment");

		view.Lines.Clear();
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 2, ShouldSmoothPathWhenDrawn = false, Granularity = 5, Points = new(raw) });
		var rawImage = await RenderAsync(view);
		DrawingPixelAssert.ColorAtUnits(rawImage, probe.X, probe.Y, red, because: "the unsmoothed line follows the raw segment");
	});

	// Catches: Initialize does not assign the native drawable
	[Fact]
	public Task InitializeAssignsTheNativeDrawable() => WithViewAsync(async view =>
	{
		var uninitialized = await InvokeOnMainThreadAsync(() => DrawingViewTestPlatform.CreateView(50, 50));

		Assert.NotNull(DrawingViewTestPlatform.GetDrawable(view));
		Assert.Null(DrawingViewTestPlatform.GetDrawable(uninitialized));
	});

	// Catches: Initialize does not subscribe to Lines changes (no redraw)
	[Fact]
	public Task AddingALineRequestsARedraw()
	{
		var counter = new DrawCounter();

		return WithViewAsync(
			view => AssertRedrawRequestedAsync(counter, () => view.Lines.Add(new MauiDrawingLine { Points = [new(10, 10), new(50, 50)] }), "Adding a line did not redraw the view."),
			view => view.DrawAction = counter.Draw);
	}

	// Catches: the Paint setter does not request a redraw
	[Fact]
	public Task SettingPaintRequestsARedraw()
	{
		var counter = new DrawCounter();

		return WithViewAsync(
			view => AssertRedrawRequestedAsync(counter, () => view.Paint = new SolidPaint(red), "Setting Paint did not redraw the view."),
			view => view.DrawAction = counter.Draw);
	}

	// Catches: a gesture step does not request a redraw
	[Fact]
	public Task EveryGestureStepRequestsARedraw()
	{
		var counter = new DrawCounter();

		return WithViewAsync(async view =>
		{
			await AssertRedrawRequestedAsync(counter, () => DrawingViewTestPlatform.TouchDown(view, 20, 20), "Touch down did not redraw the view.");
			await AssertRedrawRequestedAsync(counter, () => DrawingViewTestPlatform.TouchMove(view, 60, 60), "Touch move did not redraw the view.");
			await AssertRedrawRequestedAsync(counter, () => DrawingViewTestPlatform.TouchCancel(view, 60, 60), "Touch cancel did not redraw the view.");
		}, view => view.DrawAction = counter.Draw);
	}

	// Catches: CleanUp does not unsubscribe from Lines changes (a changed Lines collection would still clear the in-progress path)
	[Fact]
	public Task CleanUpStopsLineChangesFromClearingTheInProgressPath() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.LineColor = red;
		view.LineWidth = 10;

		// control: while the view listens to its Lines, a change drops the in-progress path (LoadLines clears it)
		DrawingViewTestPlatform.TouchDown(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);
		DrawingPixelAssert.ContainsColor(await RenderAsync(view), red, because: "the in-progress path is drawn");
		view.Lines.Add(new MauiDrawingLine { Points = [] });
		DrawingPixelAssert.DoesNotContainColor(await RenderAsync(view), red, because: "control: the changed Lines cleared the in-progress path");
		DrawingViewTestPlatform.TouchCancel(view, 180, 50);

		// after CleanUp (twice: it is idempotent) the same change leaves the in-progress path alone
		DrawingViewTestPlatform.TouchDown(view, 20, 50);
		DrawingViewTestPlatform.TouchMove(view, 180, 50);
		view.CleanUp();
		view.CleanUp();
		view.Lines.Add(new MauiDrawingLine { Points = [] });

		DrawingPixelAssert.ContainsColor(await RenderAsync(view), red, because: "CleanUp unsubscribed from the Lines changes");
	});

	// Catches: strokes are not drawn with round caps (butt or square caps) or round joins (miter or bevel joins)
	[Fact]
	public Task StoredLineCapsAndJoinsAreRounded() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		// a horizontal 20 wide line: its round caps are half circles of radius 10 around (120,50) and (170,50)
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 20, ShouldSmoothPathWhenDrawn = false, Points = [new(120, 50), new(170, 50)] });
		// a 14 wide "V" with its corner at (50,25): a round join reaches 7 units beyond the corner, a miter join 17, a bevel join 3
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 14, ShouldSmoothPathWhenDrawn = false, Points = [new(30, 70), new(50, 25), new(70, 70)] });

		var image = await RenderAsync(view);

		DrawingPixelAssert.ColorAtUnits(image, 113, 50, red, because: "a butt cap would end at x = 120");
		DrawingPixelAssert.ColorAtUnits(image, 177, 50, red, because: "a butt cap would end at x = 170");
		DrawingPixelAssert.ColorAtUnits(image, 112, 42, white, because: "a square cap would cover the corner of the cap, which is outside the round cap");
		DrawingPixelAssert.ColorAtUnits(image, 178, 58, white, because: "a square cap would cover the corner of the cap, which is outside the round cap");
		DrawingPixelAssert.ColorAtUnits(image, 50, 19.5, red, because: "a bevel join would stop 3 units beyond the corner");
		DrawingPixelAssert.ColorAtUnits(image, 50, 12, white, because: "a miter join would reach 17 units beyond the corner");
	});

	// Catches: SetStroke does not reset the dash pattern, so strokes inherit the dashes that a DrawAction left on the canvas
	[Fact]
	public Task StoredLinesAndTheInProgressPathAreSolidWhenTheDrawActionLeavesADashPattern() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.IsMultiLineModeEnabled = true; // otherwise starting the gesture clears the stored line
		view.LineColor = blue;
		view.LineWidth = 4;
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 4, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 25), new(180, 25)] });
		DrawingViewTestPlatform.TouchDown(view, 20, 75);
		DrawingViewTestPlatform.TouchMove(view, 180, 75);

		var image = await RenderAsync(view);

		// the dashes are at least 5 units long and the gaps at least 11 units wide (whether MAUI scales the pattern by the stroke size or not), so a dashed stroke misses a sample every 10 units
		DrawingPixelAssert.SegmentHasColor(image, (20, 25), (180, 25), red, samples: 15, because: "the stored line is solid");
		DrawingPixelAssert.SegmentHasColor(image, (20, 75), (180, 75), blue, samples: 15, because: "the in-progress path is solid");
	}, view => view.DrawAction = (canvas, _) => canvas.StrokeDashPattern = [5f, 15f]);

	// Catches: the lines are painted with a gradient that the DrawAction left as the fill paint (see StoredLinesAndTheInProgressPathKeepTheirColorAboveAGradientBackground)
	[Fact]
	public Task StoredLinesAndTheInProgressPathKeepTheirColorWhenTheDrawActionLeavesAGradientFillPaint() => WithViewAsync(async view =>
	{
		view.Paint = new SolidPaint(white);
		view.DrawAction = (canvas, dirtyRect) =>
		{
			canvas.SetFillPaint(CreateWhiteToYellowGradient("linear"), dirtyRect);
			canvas.FillRectangle(0, 40, dirtyRect.Width, 20);
		};
		view.IsMultiLineModeEnabled = true; // otherwise starting the gesture clears the stored line
		view.LineColor = blue;
		view.LineWidth = 10;
		view.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 10, ShouldSmoothPathWhenDrawn = false, Points = [new(20, 25), new(180, 25)] });
		DrawingViewTestPlatform.TouchDown(view, 20, 75);
		DrawingViewTestPlatform.TouchMove(view, 180, 75);

		var image = await RenderAsync(view);

		DrawingPixelAssert.SegmentHasColor(image, (20, 25), (180, 25), red, because: "the stored line has its own color");
		DrawingPixelAssert.SegmentHasColor(image, (20, 75), (180, 75), blue, because: "the in-progress path has the view's LineColor");
		DrawingPixelAssert.ColorAtUnits(image, 197, 50, Colors.Yellow, tolerance: 60, because: "the DrawAction's gradient band is drawn");
	});

	// Catches: OnMoving dereferences a null current line (defensive branch, forced with reflection because input cannot reach it)
	[Fact]
	public Task MoveWhileDrawingWithoutACurrentLineStillReportsThePoint() => WithView(view =>
	{
		var recorder = new GestureRecorder(view);
		typeof(MauiDrawingView).GetField("isDrawing", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(view, true);

		DrawingViewTestPlatform.TouchMove(view, 25, 25);

		AssertPoint(25, 25, Assert.Single(recorder.Points));
		Assert.Empty(recorder.Completed);
	});

	// Catches: CleanUp does not dispose the in-progress path, so the native path that the platform canvas cached in PathF.PlatformPath is never released
	[Fact]
	public async Task CleanUpDisposesTheInProgressPath()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();
			var cachedPlatformPath = new PlatformPathProbe();
			GetCurrentPath(view).PlatformPath = cachedPlatformPath;

			view.CleanUp();

			Assert.True(cachedPlatformPath.IsDisposed, "CleanUp did not dispose the in-progress PathF (PathF.Dispose releases its PlatformPath).");
			Assert.Null(GetCurrentPath(view).PlatformPath);
		});
	}

	// The view must not stay reachable after use and CleanUp
	[Fact]
	public async Task PlatformViewDoesNotLeak()
	{
		SetupBuilder();
		WeakReference viewReference = null!, drawableReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = DrawingViewTestPlatform.CreateView(100, 100);
			view.Initialize();
			var recorder = new GestureRecorder(view);
			DrawingViewTestPlatform.TouchDown(view, 10, 10);
			DrawingViewTestPlatform.TouchMove(view, 30, 30);
			DrawingViewTestPlatform.TouchUp(view, 30, 30);
			Assert.Single(recorder.Completed);
			view.CleanUp();
			viewReference = new(view);
			drawableReference = new(DrawingViewTestPlatform.GetDrawable(view));
		});

		await AssertionExtensions.WaitForGC(viewReference, drawableReference);
	}

	// AttachedToWindow keeps its subscribers weakly, like the other events of the view: while a DrawingView is unloaded its platform view stays in the view
	// hierarchy of the page, and it must not keep the handler that waits for the event alive. A subscriber that is kept elsewhere still receives the event.
	// Catches: AttachedToWindow keeping a strong reference to its subscribers.
	[Fact]
	public async Task AttachedToWindowDoesNotKeepItsSubscriberAlive()
	{
		SetupBuilder();
		MauiDrawingView view = null!;
		AttachedToWindowRecorder keptSubscriber = null!;
		WeakReference droppedSubscriber = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			view = DrawingViewTestPlatform.CreateView(100, 100);
			keptSubscriber = new AttachedToWindowRecorder(view);
			droppedSubscriber = new(new AttachedToWindowRecorder(view));
		});

		await AssertionExtensions.WaitForGC(droppedSubscriber);
		await InvokeOnMainThreadAsync(view.RaiseAttachedToWindow);
		Assert.Equal(1, keptSubscriber.Count);
	}

	Task WithView(Action<MauiDrawingView> body, Action<MauiDrawingView>? configure = null) =>
		WithViewAsync(view =>
		{
			body(view);
			return Task.CompletedTask;
		}, configure);

	// Creates and initializes a view, lets the test configure it, hosts it in the runner's window and runs the body on the UI thread.
	// Properties that are set before hosting (DrawAction) are in effect for the view's first draw.
	async Task WithViewAsync(Func<MauiDrawingView, Task> body, Action<MauiDrawingView>? configure = null, double width = DrawingViewTestPlatform.DefaultWidth, double height = DrawingViewTestPlatform.DefaultHeight)
	{
		SetupBuilder();
		var view = await InvokeOnMainThreadAsync(() =>
		{
			var created = DrawingViewTestPlatform.CreateView(width, height);
			created.Initialize();
			configure?.Invoke(created);
			return created;
		});

		await DrawingViewTestPlatform.HostAsync(view, MauiContext, async () =>
		{
			await body(view);
			return true;
		});
	}

	Task<DrawingPixelImage> RenderAsync(MauiDrawingView view) => DrawingViewTestPlatform.RenderAsync(view, MauiContext);

	static void Stroke(MauiDrawingView view, params (double X, double Y)[] points)
	{
		DrawingViewTestPlatform.TouchDown(view, points[0].X, points[0].Y);
		foreach (var point in points.Skip(1))
		{
			DrawingViewTestPlatform.TouchMove(view, point.X, point.Y);
		}

		DrawingViewTestPlatform.TouchUp(view, points[^1].X, points[^1].Y);
	}

	static double DistanceToPolyline(PointF point, IList<PointF> polyline)
	{
		var best = double.MaxValue;
		for (var i = 0; i < polyline.Count - 1; i++)
		{
			double ax = polyline[i].X, ay = polyline[i].Y, bx = polyline[i + 1].X, by = polyline[i + 1].Y;
			double dx = bx - ax, dy = by - ay;
			var lengthSquared = (dx * dx) + (dy * dy);
			var t = lengthSquared is 0 ? 0 : Math.Clamp((((point.X - ax) * dx) + ((point.Y - ay) * dy)) / lengthSquared, 0, 1);
			best = Math.Min(best, Math.Sqrt(Math.Pow(point.X - (ax + (t * dx)), 2) + Math.Pow(point.Y - (ay + (t * dy)), 2)));
		}

		return best;
	}

	static void AssertPoint(double expectedX, double expectedY, PointF actual)
	{
		Assert.InRange(actual.X, expectedX - 0.01, expectedX + 0.01);
		Assert.InRange(actual.Y, expectedY - 0.01, expectedY + 0.01);
	}

	static Task<int> WaitForDrawsToSettleAsync(DrawCounter counter) => DrawingViewTestPlatform.WaitForDrawsToSettleAsync(() => counter.Count);

	static async Task AssertRedrawRequestedAsync(DrawCounter counter, Action action, string message)
	{
		var before = await WaitForDrawsToSettleAsync(counter);
		action();
		await AssertionExtensions.AssertEventually(() => counter.Count > before, timeout: 3000, message: message);
	}

	// The view's in-progress path (private MauiDrawingView.currentPath).
	static PathF GetCurrentPath(MauiDrawingView view) =>
		(PathF)typeof(MauiDrawingView).GetField("currentPath", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;

	// A gradient in colors that no line in these tests uses: white at the left edge to yellow at the right edge (along y = 50 for both kinds)
	static Paint CreateWhiteToYellowGradient(string kind) => kind switch
	{
		"linear" => new LinearGradientPaint([new PaintGradientStop(0, white), new PaintGradientStop(1, Colors.Yellow)], new Point(0, 0.5), new Point(1, 0.5)),
		"radial" => new RadialGradientPaint(new Point(0, 0.5), 1) { GradientStops = [new PaintGradientStop(0, white), new PaintGradientStop(1, Colors.Yellow)] },
		_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
	};

	// Stands for the native path (an Android Path, a CGPath, a Win2D geometry) that a platform canvas caches in PathF.PlatformPath:
	// PathF.Dispose releases it (Microsoft.Maui.Graphics 10.0.90: Dispose -> ReleaseNative -> PlatformPath.Dispose()).
	sealed class PlatformPathProbe : IDisposable
	{
		public bool IsDisposed { get; private set; }

		public void Dispose() => IsDisposed = true;
	}

	// Event handlers are plain instance methods: MAUI's WeakEventManager keeps only a weak reference to a handler's target, so a
	// lambda that captures locals (a compiler-generated closure) could be collected and silently stop receiving events.
	sealed class GestureRecorder
	{
		readonly MauiDrawingView view;

		public GestureRecorder(MauiDrawingView view)
		{
			this.view = view;
			view.DrawingStarted += OnDrawingStarted;
			view.Drawing += OnDrawing;
			view.DrawingLineCompleted += OnDrawingLineCompleted;
			view.DrawingCancelled += OnDrawingCancelled;
		}

		public List<PointF> Started { get; } = [];

		public List<PointF> Points { get; } = [];

		public List<MauiDrawingLine> Completed { get; } = [];

		public List<MauiDrawingLine[]> LinesWhenCompleted { get; } = [];

		public int Cancelled { get; private set; }

		public int Count(string eventName) => eventName switch
		{
			nameof(MauiDrawingView.DrawingStarted) => Started.Count,
			nameof(MauiDrawingView.Drawing) => Points.Count,
			nameof(MauiDrawingView.DrawingLineCompleted) => Completed.Count,
			nameof(MauiDrawingView.DrawingCancelled) => Cancelled,
			_ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null)
		};

		public void Unsubscribe(string eventName)
		{
			switch (eventName)
			{
				case nameof(MauiDrawingView.DrawingStarted):
					view.DrawingStarted -= OnDrawingStarted;
					break;
				case nameof(MauiDrawingView.Drawing):
					view.Drawing -= OnDrawing;
					break;
				case nameof(MauiDrawingView.DrawingLineCompleted):
					view.DrawingLineCompleted -= OnDrawingLineCompleted;
					break;
				case nameof(MauiDrawingView.DrawingCancelled):
					view.DrawingCancelled -= OnDrawingCancelled;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(eventName), eventName, null);
			}
		}

		void OnDrawingStarted(object? sender, MauiDrawingStartedEventArgs e) => Started.Add(e.Point);

		void OnDrawing(object? sender, MauiOnDrawingEventArgs e) => Points.Add(e.Point);

		void OnDrawingLineCompleted(object? sender, MauiDrawingLineCompletedEventArgs e)
		{
			Completed.Add(e.Line);
			LinesWhenCompleted.Add([.. view.Lines]);
		}

		// EventArgs: a handler for the base type of the declared MauiDrawingStartedEventArgs (TypedCancelRecorder uses the declared signature).
		void OnDrawingCancelled(object? sender, EventArgs e) => Cancelled++;
	}

	// Counts the AttachedToWindow events of a view (a plain instance method as the handler: the view keeps its subscribers weakly).
	sealed class AttachedToWindowRecorder
	{
		public AttachedToWindowRecorder(MauiDrawingView view) => view.AttachedToWindow += OnAttachedToWindow;

		public int Count { get; private set; }

		void OnAttachedToWindow(object? sender, EventArgs e) => Count++;
	}

	// A DrawingCancelled handler that uses the event's declared signature.
	sealed class TypedCancelRecorder
	{
		public TypedCancelRecorder(MauiDrawingView view) => view.DrawingCancelled += OnDrawingCancelled;

		public int Cancelled => Points.Count;

		public List<PointF> Points { get; } = [];

		void OnDrawingCancelled(object? sender, MauiDrawingStartedEventArgs e) => Points.Add(e.Point);
	}

	sealed class DrawCounter
	{
		int count;

		public int Count => Volatile.Read(ref count);

		public List<RectF> Rects { get; } = [];

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			Interlocked.Increment(ref count);
			Rects.Add(dirtyRect);
		}
	}
}