using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reflection;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.DrawingView)]
[Collection(TestCollections.DisableParallelization)]
public partial class DrawingViewHandlerTests : CommunityToolkitHandlerTestBase
{
	static readonly Color red = Colors.Red;
	static readonly Color blue = Colors.Blue;
	static readonly Color white = Colors.White;

	const string unloadedNotRaisedReason = "The host did not raise VisualElement.Unloaded for the DrawingView after it left the window (observed on iOS and Mac Catalyst in the headless runner, which has no MAUI Window). DrawingView.OnDrawingViewUnloaded is covered on every platform by UnloadedEventStopsTheHandlerListeningAndTheDrawingViewKeepsListening, which raises the event itself.";

	const string unloadedFieldMissingReason = "MAUI's VisualElement no longer keeps its Unloaded subscribers in the private _unloaded field, so the event cannot be raised without a window.";

	const string loadedFieldMissingReason = "MAUI's VisualElement no longer keeps its Loaded subscribers in the private _loaded field, so the event cannot be raised while the view stays in the window.";

	// MAUI 10 keeps the Loaded and Unloaded subscribers of a VisualElement in these private delegate fields (verified in Microsoft.Maui.Controls 10.0.90).
	static readonly FieldInfo? unloadedField = typeof(VisualElement).GetField("_unloaded", BindingFlags.Instance | BindingFlags.NonPublic);

	static readonly FieldInfo? loadedField = typeof(VisualElement).GetField("_loaded", BindingFlags.Instance | BindingFlags.NonPublic);

	[Fact]
	public Task HandlerCreatesAnInitializedMauiDrawingViewForTheDrawingView()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			Assert.IsType<MauiDrawingView>(platformView);
			Assert.Same(drawingView, handler.VirtualView);
			Assert.Same(handler, drawingView.Handler);
			Assert.NotNull(DrawingViewTestPlatform.GetDrawable(platformView));
		});
	}

	[Fact]
	public async Task DrawingViewResolvesTheHandlerThatTheToolkitRegisters()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler(drawingView);

			var drawingViewHandler = Assert.IsType<DrawingViewHandler>(handler);
			Assert.IsType<MauiDrawingView>(drawingViewHandler.PlatformView);
			Assert.Same(drawingView, drawingViewHandler.VirtualView);
			handler.DisconnectHandler();
		});
	}

	[Fact]
	public Task DefaultDrawingViewValuesReachThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.Stroke(platformView, (20, 20), (60, 60));
			DrawingViewTestPlatform.Stroke(platformView, (20, 80), (60, 80));

			Assert.Equal(2, recorder.Completed.Count);
			var line = Assert.Single(platformView.Lines);
			Assert.Equal(Colors.Black, line.LineColor);
			Assert.Equal(5f, line.LineWidth);
			DrawingPixelAssert.ColorAtUnits(await RenderAsync(platformView), 150, 50, Colors.LightGray, because: "the default background is LightGray");
		});
	}

	// MapLines reads the multi-line mode from the DrawingView, so whether preloaded lines are trimmed to one line does not depend on the order of the mappers.
	[Fact]
	public Task InitialPropertyValuesAreAppliedToThePlatformViewWithLinesMappedLast()
	{
		var drawingView = CreateDrawingView();
		drawingView.LineColor = red;
		drawingView.LineWidth = 8;
		drawingView.IsMultiLineModeEnabled = true;
		drawingView.Background = new SolidColorBrush(white);
		drawingView.DrawAction = (canvas, _) =>
		{
			canvas.FillColor = blue;
			canvas.FillRectangle(150, 10, 30, 30);
		};
		drawingView.Lines.Add(CreateLine(red, 6, (10, 20), (90, 20)));
		drawingView.Lines.Add(CreateLine(blue, 6, (10, 50), (90, 50)));
		drawingView.Lines.Add(CreateLine(red, 6, (10, 80), (90, 80)));

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			Assert.Equal(3, platformView.Lines.Count);
			AssertLine(platformView.Lines[1], blue, 6, (10, 50), (90, 50));
			Assert.Equal(3, drawingView.Lines.Count);

			DrawingViewTestPlatform.Stroke(platformView, (100, 90), (140, 90));

			var line = platformView.Lines[^1];
			Assert.Equal(4, platformView.Lines.Count);
			Assert.Equal(red, line.LineColor);
			Assert.Equal(8f, line.LineWidth);
			var image = await RenderAsync(platformView);
			DrawingPixelAssert.ColorAtUnits(image, 5, 5, white);
			DrawingPixelAssert.NotColorAtUnits(image, 50, 20, white, because: "a pre-populated line is rendered");
			DrawingPixelAssert.ColorAtUnits(image, 165, 25, blue, because: "the DrawAction output is rendered");
		});
	}

	[Fact]
	public Task ShouldClearOnFinishChangesAreAppliedToThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);
			drawingView.ShouldClearOnFinish = true;

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));

			Assert.Single(recorder.Completed);
			Assert.Empty(platformView.Lines);
			Assert.Empty(drawingView.Lines);

			drawingView.ShouldClearOnFinish = false;
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));

			Assert.Equal(2, recorder.Completed.Count);
			Assert.Single(platformView.Lines);
			Assert.Single(drawingView.Lines);
		});
	}

	[Fact]
	public Task IsMultiLineModeEnabledChangesAreAppliedToThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 10));
			DrawingViewTestPlatform.Stroke(platformView, (10, 30), (50, 30));
			Assert.Single(platformView.Lines);

			drawingView.IsMultiLineModeEnabled = true;
			DrawingViewTestPlatform.Stroke(platformView, (10, 50), (50, 50));
			DrawingViewTestPlatform.Stroke(platformView, (10, 70), (50, 70));
			Assert.Equal(3, platformView.Lines.Count);

			drawingView.IsMultiLineModeEnabled = false;
			DrawingViewTestPlatform.Stroke(platformView, (10, 90), (50, 90));
			Assert.Single(platformView.Lines);
		});
	}

	[Fact]
	public Task LineColorAndLineWidthChangesAreAppliedToNewLines()
	{
		var drawingView = CreateDrawingView();

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);
			drawingView.Background = new SolidColorBrush(white);
			drawingView.LineColor = red;
			drawingView.LineWidth = 12;

			DrawingViewTestPlatform.Stroke(platformView, (20, 50), (180, 50));

			var completed = Assert.Single(recorder.Completed);
			Assert.Equal(red, completed.LineColor);
			Assert.Equal(12f, completed.LineWidth);
			Assert.Equal(red, platformView.Lines[0].LineColor);
			Assert.Equal(12f, platformView.Lines[0].LineWidth);
			var image = await RenderAsync(platformView);
			DrawingPixelAssert.ColorAtUnits(image, 100, 54, red, because: "12 units wide: covers y 44..56");
			DrawingPixelAssert.ColorAtUnits(image, 100, 64, white);
		});
	}

	[Fact]
	public Task DrawActionChangesAreAppliedToThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			var counter = new DrawCounter();
			drawingView.Background = new SolidColorBrush(white);
			drawingView.DrawAction = (canvas, rect) =>
			{
				counter.Draw(canvas, rect);
				canvas.FillColor = blue;
				canvas.FillRectangle(20, 20, 40, 40);
			};
			drawingView.Background = new SolidColorBrush(Colors.Yellow);

			var image = await RenderAsync(platformView);
			Assert.True(counter.Count > 0);
			DrawingPixelAssert.ColorAtUnits(image, 40, 40, blue);
			DrawingPixelAssert.ColorAtUnits(image, 150, 50, Colors.Yellow);

			drawingView.DrawAction = null;
			drawingView.Background = new SolidColorBrush(white);
			var invoked = counter.Count;

			image = await RenderAsync(platformView);
			Assert.Equal(invoked, counter.Count);
			DrawingPixelAssert.ColorAtUnits(image, 40, 40, white);
		});
	}

	[Fact]
	public Task AssigningDrawActionOnTheDrawingViewRedrawsThePlatformView()
	{
		var drawingView = CreateDrawingView();
		var first = new DrawCounter();
		var second = new DrawCounter();
		drawingView.DrawAction = first.Draw;

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			await DrawingViewTestPlatform.WaitForDrawsToSettleAsync(() => first.Count);

			drawingView.DrawAction = second.Draw;

			Assert.Same(second, platformView.DrawAction?.Target);
			await AssertionExtensions.AssertEventually(() => second.Count > 0, timeout: 3000, message: "The new DrawAction was never invoked: assigning DrawingView.DrawAction did not redraw the platform view.");
		});
	}

	[Fact]
	public Task BackgroundChangesAreAppliedAndNullFallsBackToLightGray()
	{
		var drawingView = CreateDrawingView();

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			drawingView.Background = new SolidColorBrush(red);
			DrawingPixelAssert.ColorAtUnits(await RenderAsync(platformView), 100, 50, red);

			drawingView.Background = new LinearGradientBrush(
				[new GradientStop(blue, 0), new GradientStop(white, 1)], new Point(0, 0.5), new Point(1, 0.5));
			var gradient = await RenderAsync(platformView);
			DrawingPixelAssert.ColorAtUnits(gradient, 3, 50, blue, tolerance: 60);
			DrawingPixelAssert.ColorAtUnits(gradient, 197, 50, white, tolerance: 60);

			// IView.Background returns the BackgroundColor that the DrawingView constructor sets (LightGray) as a brush, so it is not null here.
			drawingView.Background = null;
			Assert.NotNull(((IView)drawingView).Background);
			DrawingPixelAssert.ColorAtUnits(await RenderAsync(platformView), 100, 50, Colors.LightGray);

			// With no BackgroundColor either, IView.Background is null and MapDrawingViewBackground falls back to DrawingViewDefaults.BackgroundColor.
			drawingView.Background = new SolidColorBrush(red); // a different color first, so the fallback has to repaint the view
			drawingView.BackgroundColor = null;
			drawingView.Background = null;
			Assert.Null(((IView)drawingView).Background);
			DrawingPixelAssert.ColorAtUnits(await RenderAsync(platformView), 100, 50, Colors.LightGray);
		});
	}

	[Fact]
	public async Task CustomMappersReplaceTheDefaultMappersAndNullSelectsTheDefaults()
	{
		SetupBuilder();
		// Which command mapper a handler uses has no native observable without focus or window setup, so it is read from the internal readonly
		// field that MAUI keeps it in (ElementHandler._commandMapper, Microsoft.Maui 10.0.90).
		var commandMapperField = typeof(ElementHandler).GetField("_commandMapper", BindingFlags.Instance | BindingFlags.NonPublic);
		Skip.If(commandMapperField is null, "MAUI's ElementHandler no longer keeps the command mapper in the _commandMapper field.");
		var mappedLineColors = new List<Color>();
		var focusRequests = new List<object?>();
		var mapper = new PropertyMapper<IDrawingView, DrawingViewHandler>
		{
			[nameof(IDrawingView.LineColor)] = (handler, view) => mappedLineColors.Add(view.LineColor)
		};
		var commandMapper = new CommandMapper<IDrawingView, DrawingViewHandler>
		{
			[nameof(IView.Focus)] = (handler, view, args) => focusRequests.Add(args)
		};
		var customView = CreateDrawingView();
		customView.LineColor = red;
		customView.LineWidth = 11;
		var defaultView = CreateDrawingView();
		defaultView.LineWidth = 11;

		await InvokeOnMainThreadAsync(() =>
		{
			var customHandler = new DrawingViewHandler(mapper, commandMapper);
			InitializeViewHandler(customView, customHandler);
			var request = new FocusRequest();
			((IElementHandler)customHandler).Invoke(nameof(IView.Focus), request);

			Assert.Equal(red, Assert.Single(mappedLineColors));
			Assert.Same(request, Assert.Single(focusRequests));
			DrawingViewTestPlatform.Stroke(customHandler.PlatformView, (10, 10), (30, 30));
			Assert.Equal(5f, Assert.Single(customHandler.PlatformView.Lines).LineWidth);

			var defaultHandler = new DrawingViewHandler(null, null);
			InitializeViewHandler(defaultView, defaultHandler);
			DrawingViewTestPlatform.Stroke(defaultHandler.PlatformView, (10, 10), (30, 30));
			Assert.Equal(11f, Assert.Single(defaultHandler.PlatformView.Lines).LineWidth);
			Assert.Same(DrawingViewHandler.DrawingViewCommandMapper, commandMapperField.GetValue(defaultHandler));
			Assert.Same(commandMapper, commandMapperField.GetValue(customHandler));
			Assert.Same(DrawingViewHandler.DrawingViewCommandMapper, commandMapperField.GetValue(new DrawingViewHandler()));

			((IElementHandler)customHandler).DisconnectHandler();
			((IElementHandler)defaultHandler).DisconnectHandler();
		});
	}

	[Fact]
	public Task PlatformGestureEventsAreRelayedToTheDrawingViewEvents()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.TouchDown(platformView, 10, 20);
			DrawingViewTestPlatform.TouchMove(platformView, 30, 40);
			DrawingViewTestPlatform.TouchMove(platformView, 50, 60);
			DrawingViewTestPlatform.TouchUp(platformView, 50, 60);
			DrawingViewTestPlatform.TouchDown(platformView, 70, 80);
			DrawingViewTestPlatform.TouchCancel(platformView, 70, 80);

			Assert.Equal(2, recorder.Started.Count);
			AssertPoint(10, 20, recorder.Started[0]);
			AssertPoint(70, 80, recorder.Started[1]);
			Assert.Equal(2, recorder.Points.Count);
			AssertPoint(30, 40, recorder.Points[0]);
			AssertPoint(50, 60, recorder.Points[1]);
			var line = Assert.Single(recorder.Completed);
			Assert.Equal(3, line.Points.Count);
			Assert.Equal(1, recorder.Cancelled);
		});
	}

	[Fact]
	public Task DrawingViewCommandsAreExecutedWithTheEventData()
	{
		var drawingView = CreateDrawingView();
		var started = new List<PointF>();
		var points = new List<PointF>();
		var completed = new List<IDrawingLine>();
		var cancelled = 0;
		var blockedExecutions = 0;
		drawingView.DrawingLineStartedCommand = new Command<PointF>(started.Add);
		drawingView.PointDrawnCommand = new Command<PointF>(points.Add);
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(completed.Add);
		drawingView.DrawingLineCancelledCommand = new Command(() => cancelled++);

		return Run(drawingView, (handler, platformView) =>
		{
			DrawingViewTestPlatform.Stroke(platformView, (10, 20), (30, 40));
			DrawingViewTestPlatform.TouchDown(platformView, 5, 5);
			DrawingViewTestPlatform.TouchCancel(platformView, 5, 5);

			Assert.Equal(2, started.Count);
			AssertPoint(10, 20, started[0]);
			AssertPoint(30, 40, Assert.Single(points));
			Assert.Equal(2, Assert.Single(completed).Points.Count);
			Assert.Equal(1, cancelled);

			drawingView.DrawingLineStartedCommand = new Command<PointF>(_ => blockedExecutions++, _ => false);
			drawingView.PointDrawnCommand = new Command<PointF>(_ => blockedExecutions++, _ => false);
			drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(_ => blockedExecutions++, _ => false);
			drawingView.DrawingLineCancelledCommand = new Command(() => blockedExecutions++, () => false);
			DrawingViewTestPlatform.Stroke(platformView, (10, 20), (30, 40));
			DrawingViewTestPlatform.TouchDown(platformView, 5, 5);
			DrawingViewTestPlatform.TouchCancel(platformView, 5, 5);
			Assert.Equal(0, blockedExecutions);
		});
	}

	[Fact]
	public Task CompletedLinesAreConvertedWithTheDefaultAdapter()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);
			drawingView.LineColor = blue;
			drawingView.LineWidth = 9;

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (40, 20), (70, 30));

			var completed = Assert.IsType<DrawingLine>(Assert.Single(recorder.Completed));
			var platformLine = Assert.Single(platformView.Lines);
			Assert.Equal(blue, completed.LineColor);
			Assert.Equal(9f, completed.LineWidth);
			Assert.Equal(platformLine.Granularity, completed.Granularity);
			Assert.Equal(platformLine.ShouldSmoothPathWhenDrawn, completed.ShouldSmoothPathWhenDrawn);
			Assert.Equal(platformLine.Points, completed.Points);
			Assert.NotSame(platformLine.Points, completed.Points);
			var synced = Assert.IsType<DrawingLine>(Assert.Single(drawingView.Lines));
			Assert.Equal(platformLine.Points, synced.Points);
		});
	}

	[Fact]
	public Task CustomAdapterConvertsCompletedAndSynchronizedLines()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);
			var adapter = new TaggingAdapter();
			handler.SetDrawingLineAdapter(adapter);

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (40, 20));

			Assert.IsType<TaggedDrawingLine>(Assert.Single(recorder.Completed));
			Assert.IsType<TaggedDrawingLine>(Assert.Single(drawingView.Lines));
			Assert.NotEmpty(adapter.Converted);
		});
	}

	// An app must be able to act on the line it is told about, for example an undo with drawingView.Lines.Remove(e.LastDrawingLine), and DrawingLine has reference equality.
	[Fact]
	public Task CompletedLineIsTheInstanceStoredInDrawingViewLines()
	{
		var drawingView = CreateDrawingView();
		var commandLines = new List<IDrawingLine>();
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(commandLines.Add);

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));

			var completed = Assert.Single(recorder.Completed);
			Assert.Same(completed, Assert.Single(commandLines));
			Assert.Same(completed, Assert.Single(drawingView.Lines));

			Assert.True(drawingView.Lines.Remove(completed));
			Assert.Empty(platformView.Lines);
		});
	}

	// In multi-line mode every stroke copies the platform lines to DrawingView.Lines again, which could replace the instances reported for earlier strokes.
	[Fact]
	public Task CompletedLinesStayTheInstancesInDrawingViewLinesAfterLaterStrokes()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));
			DrawingViewTestPlatform.Stroke(platformView, (60, 10), (100, 50));
			DrawingViewTestPlatform.Stroke(platformView, (110, 10), (150, 50));

			Assert.Equal(3, recorder.Completed.Count);
			Assert.Equal(3, drawingView.Lines.Count);
			Assert.Equal(3, platformView.Lines.Count);
			for (var i = 0; i < recorder.Completed.Count; i++)
			{
				Assert.Same(recorder.Completed[i], drawingView.Lines[i]);
			}
		});
	}

	[Fact]
	public Task CompletedLinesCanBeRemovedOneByOneInMultiLineMode()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));
			DrawingViewTestPlatform.Stroke(platformView, (60, 10), (100, 50));

			Assert.True(drawingView.Lines.Remove(recorder.Completed[1]), "Removing the line reported for the second stroke failed.");
			var remaining = Assert.Single(platformView.Lines);
			AssertPoint(10, 10, remaining.Points[0]);

			Assert.True(drawingView.Lines.Remove(recorder.Completed[0]), "Removing the line reported for the first stroke failed.");
			Assert.Empty(drawingView.Lines);
			Assert.Empty(platformView.Lines);
		});
	}

	[Fact]
	public Task PlatformLinesAreSynchronizedToTheDrawingViewLines()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (40, 20));
			var first = Assert.Single(drawingView.Lines);
			AssertPoint(10, 10, first.Points[0]);

			// single-line mode: the next gesture clears the previous line as soon as it starts
			DrawingViewTestPlatform.TouchDown(platformView, 10, 60);
			Assert.Empty(drawingView.Lines);
			DrawingViewTestPlatform.TouchMove(platformView, 40, 70);
			DrawingViewTestPlatform.TouchUp(platformView, 40, 70);
			var second = Assert.Single(drawingView.Lines);
			AssertPoint(10, 60, second.Points[0]);

			drawingView.IsMultiLineModeEnabled = true;
			DrawingViewTestPlatform.Stroke(platformView, (10, 90), (40, 95));
			Assert.Equal(2, drawingView.Lines.Count);
			Assert.Equal(2, platformView.Lines.Count);
		});
	}

	[Fact]
	public Task DrawingViewLinesAreSynchronizedToThePlatformView()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		drawingView.Background = new SolidColorBrush(white);

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			var first = CreateLine(red, 6, (10, 20), (90, 20));
			first.Granularity = 8;
			first.ShouldSmoothPathWhenDrawn = false;
			var second = CreateLine(blue, 4, (10, 60), (90, 60));

			drawingView.Lines.Add(first);
			drawingView.Lines.Add(second);

			Assert.Equal(2, platformView.Lines.Count);
			AssertLine(platformView.Lines[0], red, 6, (10, 20), (90, 20));
			Assert.Equal(8, platformView.Lines[0].Granularity);
			Assert.False(platformView.Lines[0].ShouldSmoothPathWhenDrawn);
			AssertLine(platformView.Lines[1], blue, 4, (10, 60), (90, 60));
			var image = await RenderAsync(platformView);
			DrawingPixelAssert.SegmentHasColor(image, (10, 20), (90, 20), red);
			DrawingPixelAssert.SegmentHasColor(image, (10, 60), (90, 60), blue);

			drawingView.Lines.Remove(first);
			Assert.Equal(blue, Assert.Single(platformView.Lines).LineColor);

			drawingView.Lines[0] = CreateLine(red, 9, (10, 30), (90, 30));
			AssertLine(Assert.Single(platformView.Lines), red, 9, (10, 30), (90, 30));

			drawingView.Clear();
			Assert.Empty(platformView.Lines);
			DrawingPixelAssert.DoesNotContainColor(await RenderAsync(platformView), blue);
		});
	}

	[Fact]
	public Task PlatformLinesBeyondOneAreTrimmedToTheLastLineInSingleLineMode()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			platformView.Lines.Add(new MauiDrawingLine { LineColor = red, LineWidth = 6, Points = [new(10, 20), new(90, 20)] });
			platformView.Lines.Add(new MauiDrawingLine { LineColor = blue, LineWidth = 7, Points = [new(10, 50), new(90, 50)] });

			Assert.Equal(2, platformView.Lines.Count);
			var line = Assert.Single(drawingView.Lines);
			Assert.Equal(blue, line.LineColor);
			Assert.Equal(7f, line.LineWidth);
			AssertPoint(10, 50, line.Points[0]);
		});
	}

	[Fact]
	public Task SingleLineModeKeepsOnlyTheLastDrawingViewLineOnThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			drawingView.Lines.Add(CreateLine(red, 6, (10, 20), (90, 20)));
			drawingView.Lines.Add(CreateLine(blue, 6, (10, 50), (90, 50)));
			drawingView.Lines.Add(CreateLine(red, 6, (10, 80), (90, 80)));

			var line = Assert.Single(platformView.Lines);
			AssertLine(line, red, 6, (10, 80), (90, 80));
		});
	}

	[Fact]
	public Task LinesAddedByTheAppKeepTheirIdentityInDrawingViewLines()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		var preloaded = CreateLine(red, 6, (10, 20), (90, 20));
		drawingView.Lines.Add(preloaded);

		return Run(drawingView, (handler, platformView) =>
		{
			var added = CreateLine(blue, 6, (10, 60), (90, 60));
			drawingView.Lines.Add(added);

			Assert.Equal(2, platformView.Lines.Count);
			Assert.Equal(2, drawingView.Lines.Count);
			Assert.Same(preloaded, drawingView.Lines[0]);
			Assert.Same(added, drawingView.Lines[1]);
		});
	}

	// A stroke in multi-line mode copies the platform lines to DrawingView.Lines again, which could replace the lines that the app put there (of the app's own IDrawingLine type)
	// with DrawingLine conversions of their platform copies.
	[Fact]
	public Task LinesAddedByTheAppKeepTheirIdentityAfterAStroke()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		var preloaded = new TaggedDrawingLine { LineColor = red, LineWidth = 6, ShouldSmoothPathWhenDrawn = false, Points = [new(10, 20), new(90, 20)] };
		drawingView.Lines.Add(preloaded);

		return Run(drawingView, (handler, platformView) =>
		{
			var added = new TaggedDrawingLine { LineColor = blue, LineWidth = 6, ShouldSmoothPathWhenDrawn = false, Points = [new(10, 50), new(90, 50)] };
			drawingView.Lines.Add(added);
			var recorder = new DrawingViewRecorder(drawingView);

			DrawingViewTestPlatform.Stroke(platformView, (10, 80), (90, 80));

			Assert.Equal(3, platformView.Lines.Count);
			Assert.Equal(3, drawingView.Lines.Count);
			Assert.Same(preloaded, drawingView.Lines[0]);
			Assert.Same(added, drawingView.Lines[1]);
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[2]);
		});
	}

	[Fact]
	public Task PreloadedLinesAreTrimmedToTheLastAppLineInSingleLineMode()
	{
		var drawingView = CreateDrawingView();
		drawingView.Background = new SolidColorBrush(white);
		var hidden = CreateLine(blue, 6, (10, 20), (190, 20));
		var last = CreateLine(red, 6, (10, 80), (190, 80));
		drawingView.Lines.Add(hidden);
		drawingView.Lines.Add(last);

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			Assert.Same(last, Assert.Single(drawingView.Lines));
			AssertLine(Assert.Single(platformView.Lines), red, 6, (10, 80), (190, 80));

			using var stream = await drawingView.GetImageStream(200, 100, DrawingViewOutputOption.FullCanvas);
			var image = await DecodeAsync(stream);
			DrawingPixelAssert.ColorAtUnits(image, 100, 20, white, because: "the first line is not shown in single-line mode, so it is not exported either");
			DrawingPixelAssert.NotColorAtUnits(image, 100, 80, white, because: "the last line is exported");
		});
	}

	[Fact]
	public Task LinesOfAnAssignedLinesCollectionKeepTheirIdentity()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			var appLine = new TaggedDrawingLine { LineColor = red, LineWidth = 6, ShouldSmoothPathWhenDrawn = false, Points = [new(10, 20), new(90, 20)] };
			var assigned = new ObservableCollection<IDrawingLine> { appLine };

			drawingView.Lines = assigned;

			AssertLine(Assert.Single(platformView.Lines), red, 6, (10, 20), (90, 20));
			Assert.Same(assigned, drawingView.Lines);
			Assert.Same(appLine, Assert.Single(assigned));
		});
	}

	[Fact]
	public Task LinesAddedToAReplacementLinesCollectionReachThePlatformView()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			var replacement = new ObservableCollection<IDrawingLine>();
			drawingView.Lines = replacement;

			replacement.Add(CreateLine(red, 6, (10, 20), (90, 20)));

			AssertLine(Assert.Single(platformView.Lines), red, 6, (10, 20), (90, 20));
		});
	}

	[Fact]
	public Task ChangesOfAReplacedLinesCollectionDoNotReachThePlatformView()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			var oldLines = drawingView.Lines;
			drawingView.Lines = [];
			var platformChanges = new CollectionChangeCounter(platformView.Lines);

			oldLines.Add(CreateLine(red, 6, (10, 20), (90, 20)));

			Assert.Equal(0, platformChanges.Count);
		});
	}

	// A handler is reused by IElementHandler.SetVirtualView without DisconnectHandler: it must stop observing the Lines of the previous DrawingView,
	// whose changes would otherwise rewrite the platform lines.
	[Fact]
	public Task AReusedHandlerStopsObservingTheLinesOfThePreviousDrawingView()
	{
		var first = CreateDrawingView();
		first.IsMultiLineModeEnabled = true;
		var second = CreateDrawingView();
		second.IsMultiLineModeEnabled = true;

		return Run(first, (handler, platformView) =>
		{
			((IElementHandler)handler).SetVirtualView(second);
			Assert.Same(handler, second.Handler);

			var platformChanges = new CollectionChangeCounter(platformView.Lines);
			first.Lines.Add(CreateLine(red, 6, (10, 20), (90, 20)));
			Assert.Equal(0, platformChanges.Count);

			second.Lines.Add(CreateLine(blue, 6, (10, 60), (90, 60)));
			AssertLine(Assert.Single(platformView.Lines), blue, 6, (10, 60), (90, 60));
		});
	}

	[Fact]
	public async Task AReplacedLinesCollectionCanBeChangedAfterDisconnect()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = CreateHandler<DrawingViewHandler>(drawingView);
			var oldLines = drawingView.Lines;
			drawingView.Lines = [];
			((IElementHandler)handler).DisconnectHandler();

			var exception = Record.Exception(() => oldLines.Add(CreateLine(red, 6, (10, 20), (90, 20))));

			Assert.Null(exception);
		});
	}

	// UsedHandlerViewAndPlatformViewDoNotLeakAfterDisconnect is the control for this test: the same objects are collected when Lines is not replaced.
	[Fact]
	public async Task AReplacedLinesCollectionDoesNotKeepTheHandlerAlive()
	{
		SetupBuilder();
		WeakReference handlerReference = null!, platformViewReference = null!;
		ObservableCollection<IDrawingLine> oldLines = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new DrawingView { IsMultiLineModeEnabled = true };
			oldLines = view.Lines;
			var handler = CreateHandler<DrawingViewHandler>(view);
			view.Lines = [];
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		try
		{
			await AssertionExtensions.WaitForGC(handlerReference, platformViewReference);
		}
		finally
		{
			GC.KeepAlive(oldLines);
		}
	}

	[Fact]
	public Task DisconnectHandlerStopsRelayingPlatformEvents()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			var recorder = new DrawingViewRecorder(drawingView);
			((IElementHandler)handler).DisconnectHandler();

			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));
			DrawingViewTestPlatform.TouchDown(platformView, 10, 10);
			DrawingViewTestPlatform.TouchCancel(platformView, 10, 10);

			Assert.Empty(recorder.Started);
			Assert.Empty(recorder.Points);
			Assert.Empty(recorder.Completed);
			Assert.Equal(0, recorder.Cancelled);
			Assert.Empty(drawingView.Lines);
		});
	}

	[Fact]
	public Task DisconnectHandlerStopsSynchronizingLines()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			((IElementHandler)handler).DisconnectHandler();

			drawingView.Lines.Add(CreateLine(red, 6, (10, 20), (90, 20)));
			Assert.Empty(platformView.Lines);

			DrawingViewTestPlatform.Stroke(platformView, (10, 50), (50, 50));
			Assert.Single(platformView.Lines);
			Assert.Single(drawingView.Lines);
		});
	}

	[Fact]
	public Task DisconnectHandlerCleansUpThePlatformView()
	{
		var drawingView = CreateDrawingView();
		drawingView.Background = new SolidColorBrush(white);
		drawingView.LineColor = red;
		drawingView.LineWidth = 10;

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			DrawingViewTestPlatform.TouchDown(platformView, 20, 50);
			DrawingViewTestPlatform.TouchMove(platformView, 180, 50);
			DrawingPixelAssert.ContainsColor(await RenderAsync(platformView), red, because: "the in-progress path is drawn");

			((IElementHandler)handler).DisconnectHandler();
			platformView.Lines.Add(new MauiDrawingLine { Points = [] });

			DrawingPixelAssert.ContainsColor(await RenderAsync(platformView), red, because: "CleanUp unsubscribed the platform view from its Lines, so the added line did not clear the in-progress path");
		});
	}

	[Fact]
	public Task PropertyChangesAfterDisconnectDoNotReachThePlatformView()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			((IElementHandler)handler).DisconnectHandler();

			drawingView.LineColor = red;
			drawingView.LineWidth = 17;
			drawingView.IsMultiLineModeEnabled = true;
			drawingView.ShouldClearOnFinish = true;
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));
			DrawingViewTestPlatform.Stroke(platformView, (10, 60), (50, 90));

			var line = Assert.Single(platformView.Lines); // single-line mode and no clearing: the new values did not arrive
			Assert.Equal(Colors.Black, line.LineColor);
			Assert.Equal(5f, line.LineWidth);
			AssertPoint(10, 60, line.Points[0]);
		});
	}

	[Fact]
	public async Task NewHandlerForTheSameDrawingViewTakesOverAfterDisconnect()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		drawingView.LineColor = red;
		drawingView.LineWidth = 8;

		await InvokeOnMainThreadAsync(() =>
		{
			var first = CreateHandler<DrawingViewHandler>(drawingView);
			var firstPlatformView = first.PlatformView;
			DrawingViewTestPlatform.Stroke(firstPlatformView, (10, 10), (50, 50));
			Assert.Single(drawingView.Lines);
			((IElementHandler)first).DisconnectHandler();
			Assert.Null(drawingView.Handler);

			var second = CreateHandler<DrawingViewHandler>(drawingView);
			var secondPlatformView = second.PlatformView;
			var recorder = new DrawingViewRecorder(drawingView);

			Assert.NotSame(first, second);
			Assert.NotSame(firstPlatformView, secondPlatformView);
			Assert.Same(second, drawingView.Handler);
			Assert.NotNull(DrawingViewTestPlatform.GetDrawable(secondPlatformView));
			AssertLine(Assert.Single(secondPlatformView.Lines), red, 8, (10, 10), (50, 50));
			DrawingViewTestPlatform.Stroke(secondPlatformView, (20, 60), (60, 90));
			DrawingViewTestPlatform.Stroke(firstPlatformView, (30, 70), (70, 95)); // the disconnected platform view relays nothing

			Assert.Single(recorder.Completed);
			Assert.Equal(2, drawingView.Lines.Count);
			Assert.Equal(2, secondPlatformView.Lines.Count);
			Assert.Equal(red, secondPlatformView.Lines[1].LineColor);
			Assert.Equal(8f, secondPlatformView.Lines[1].LineWidth);
			((IElementHandler)second).DisconnectHandler();
		});
	}

	[Fact]
	public Task DisconnectingTheHandlerTwiceDoesNotThrow()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			((IElementHandler)handler).DisconnectHandler();
			((IElementHandler)handler).DisconnectHandler();

			Assert.Null(drawingView.Handler);
		});
	}

	// Keeping the handler listening after Unloaded lets its platform view and the Lines that the app keeps keep each other alive: the leak that #420 fixed.
	[Fact]
	public async Task UnloadingTheDrawingViewStopsItsHandlerListening()
	{
		var drawingView = CreateDrawingView();
		var unloaded = new UnloadedRecorder(drawingView);
		DrawingViewHandler? handler = null;
		MauiDrawingView? platformView = null;

		await RunAsync(drawingView, (connectedHandler, connectedPlatformView) =>
		{
			handler = connectedHandler;
			platformView = connectedPlatformView;
			Assert.Same(connectedHandler, drawingView.Handler);
			return Task.CompletedTask;
		});
		await drawingView.OnUnloadedAsync();

		Skip.If(unloaded.Count is 0, unloadedNotRaisedReason);
		Assert.Equal(1, unloaded.Count);
		await InvokeOnMainThreadAsync(() => AssertHandlerStoppedListening(drawingView, handler!, platformView!));
	}

	// The event is raised through the delegate that MAUI's VisualElement keeps in its private _unloaded field, so the DrawingView's own subscriber runs on every
	// platform, including iOS and Mac Catalyst, where the headless runner never raises Unloaded for a view that leaves the window.
	[Fact]
	public Task UnloadedEventStopsTheHandlerListeningAndTheDrawingViewKeepsListening()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			Assert.Same(handler, drawingView.Handler);
			Assert.True(IsListeningToUnloaded(drawingView), "Precondition: the DrawingView listens to its Unloaded event.");

			RaiseUnloaded(drawingView);

			AssertHandlerStoppedListening(drawingView, handler, platformView);
			Assert.True(IsListeningToUnloaded(drawingView), "The DrawingView stopped listening to Unloaded, so it would not stop its handler listening the next time it leaves the window.");
		});
	}

	// MAUI raises Loaded right after an Unloaded that arrives while the platform view is in a window, as here, and on Android and Windows Loaded is the only
	// signal that makes the handler listen again (the platform view raises AttachedToWindow on iOS and Mac Catalyst).
	[Fact]
	public Task LoadedEventMakesTheHandlerListenAgain()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			Skip.If(loadedField is null, loadedFieldMissingReason);
			RaiseUnloaded(drawingView);
			drawingView.Lines.Add(CreateLine(red, 4, (10, 20), (190, 20)));
			Assert.Empty(platformView.Lines);

			RaiseLoaded(drawingView);

			Assert.Same(handler, drawingView.Handler);
			AssertLine(Assert.Single(platformView.Lines), red, 4, (10, 20), (190, 20));
			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(platformView, (10, 60), (50, 60));
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[^1]);
			Assert.Equal(2, platformView.Lines.Count);
		});
	}

	[Fact]
	public async Task UnloadedWithoutAHandlerKeepsTheDrawingViewListening()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			var drawingView = CreateDrawingView();
			Assert.Null(drawingView.Handler);
			Assert.True(IsListeningToUnloaded(drawingView), "Precondition: the DrawingView listens to its Unloaded event.");

			RaiseUnloaded(drawingView);

			Assert.Null(drawingView.Handler);
			Assert.True(IsListeningToUnloaded(drawingView));
		});
	}

	[Fact]
	public async Task DrawingViewStillHasAHandlerWhenItsPageIsAttachedAgain()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		var unloaded = new UnloadedRecorder(drawingView);
		var page = new ContentPage { Content = new VerticalStackLayout { drawingView } };
		DrawingViewHandler? handler = null;
		MauiDrawingView? platformView = null;

		await AttachAndRun(page, async _ =>
		{
			await page.OnLoadedAsync();
			handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
			platformView = handler.PlatformView;
		});
		await WaitUntilTheDrawingViewLeftTheWindowAsync(page, drawingView, unloaded, unloadedBefore: 0);
		await InvokeOnMainThreadAsync(() => AssertHandlerStoppedListening(drawingView, handler!, platformView!));

		await AttachAndRun(page, async _ =>
		{
			await page.OnLoadedAsync();
			await drawingView.OnLoadedAsync();

			Assert.Same(handler, drawingView.Handler);
			Assert.Same(platformView, handler!.PlatformView);
			Assert.True(handler.PlatformView.IsLoadedOnPlatform(), "The platform view of the DrawingView's handler is not the one in the window.");
			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(handler.PlatformView, (20, 20), (60, 60));
			Assert.Single(recorder.Completed);
		});
	}

	// While the page is away the handler stays connected, so a line color set then reaches the platform view right away, but it does not listen to the Lines,
	// so a line added then reaches the platform view only once the DrawingView is loaded again.
	[Fact]
	public async Task DrawingViewKeepsWorkingEachTimeItsPageIsAttachedAgain()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		var unloaded = new UnloadedRecorder(drawingView);
		var page = new ContentPage { Content = new VerticalStackLayout { drawingView } };
		DrawingViewHandler? handler = null;

		var unloadedBeforeVisit = unloaded.Count;
		await AttachAndRun(page, async _ =>
		{
			await drawingView.OnLoadedAsync();
			handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
			DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 10), (190, 10));
			Assert.Single(drawingView.Lines);
		});

		foreach (var colorSetWhileAway in new[] { red, blue })
		{
			await WaitUntilTheDrawingViewLeftTheWindowAsync(page, drawingView, unloaded, unloadedBeforeVisit);
			await InvokeOnMainThreadAsync(() =>
			{
				Assert.Same(handler, drawingView.Handler);
				var platformLineCount = handler!.PlatformView.Lines.Count;

				drawingView.LineColor = colorSetWhileAway;
				drawingView.Lines.Add(CreateLine(colorSetWhileAway, 4, (10, 50), (190, 50)));

				Assert.Equal(colorSetWhileAway, handler.PlatformView.LineColor);
				Assert.Equal(platformLineCount, handler.PlatformView.Lines.Count);
			});

			unloadedBeforeVisit = unloaded.Count;
			await AttachAndRun(page, async _ =>
			{
				await drawingView.OnLoadedAsync();

				Assert.Same(handler, drawingView.Handler);
				Assert.True(handler!.PlatformView.IsLoadedOnPlatform(), "The platform view of the DrawingView's handler is not the one in the window.");
				Assert.Equal(drawingView.Lines.Count, handler.PlatformView.Lines.Count);
				Assert.Equal(colorSetWhileAway, handler.PlatformView.Lines[^1].LineColor);
				Assert.Equal(colorSetWhileAway, handler.PlatformView.LineColor);

				var recorder = new DrawingViewRecorder(drawingView);
				DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 80), (190, 80));
				var completed = Assert.Single(recorder.Completed);
				Assert.Equal(colorSetWhileAway, completed.LineColor);
				Assert.Same(completed, drawingView.Lines[^1]);
			});
		}
	}

	// MAUI's LayoutHandler.Remove finds the platform view of a child through its handler (ElementExtensions.ToPlatform), so the DrawingView keeps its handler while its page is away.
	// Catches: the DrawingView disconnecting its handler on Unloaded (#420), which made this throw InvalidOperationException ('MauiContext should have been set on parent.').
	[Fact]
	public async Task DrawingViewCanBeRemovedFromItsLayoutWhileItsPageIsAway()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		var unloaded = new UnloadedRecorder(drawingView);
		var layout = new VerticalStackLayout { drawingView };
		var page = new ContentPage { Content = layout };

		await AttachAndRun(page, async _ => await drawingView.OnLoadedAsync());
		await WaitUntilTheDrawingViewLeftTheWindowAsync(page, drawingView, unloaded, unloadedBefore: 0);

		await InvokeOnMainThreadAsync(() =>
		{
			var handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
			var layoutPlatformView = ((IElementHandler)layout.Handler!).PlatformView!;
			Assert.True(DrawingViewTestPlatform.IsChildOf(handler.PlatformView, layoutPlatformView), "Precondition: the platform view of the DrawingView is not in the platform view of its layout while its page is away.");

			var exception = Record.Exception(() => layout.Remove(drawingView));

			Assert.Null(exception);
			Assert.Empty(layout);
			Assert.False(DrawingViewTestPlatform.IsChildOf(handler.PlatformView, layoutPlatformView), "MAUI did not remove the platform view of the DrawingView from the platform view of its layout.");
			Assert.Null(DrawingViewTestPlatform.GetParent(handler.PlatformView));
		});
	}

	[Fact]
	public async Task DrawingViewRemovedWhileItsPageIsAwayWorksInAnotherPage()
	{
		SetupBuilder();
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;
		var unloaded = new UnloadedRecorder(drawingView);
		var firstLayout = new VerticalStackLayout { drawingView };
		var firstPage = new ContentPage { Content = firstLayout };
		var otherLayout = new VerticalStackLayout();
		var otherPage = new ContentPage { Content = otherLayout };

		await AttachAndRun(firstPage, async _ =>
		{
			await drawingView.OnLoadedAsync();
			DrawingViewTestPlatform.Stroke(Assert.IsType<DrawingViewHandler>(drawingView.Handler).PlatformView, (10, 10), (190, 10));
			Assert.Single(drawingView.Lines);
		});
		await WaitUntilTheDrawingViewLeftTheWindowAsync(firstPage, drawingView, unloaded, unloadedBefore: 0);

		await InvokeOnMainThreadAsync(() =>
		{
			firstLayout.Remove(drawingView);
			drawingView.Lines.Add(CreateLine(blue, 4, (10, 50), (190, 50)));
			otherLayout.Add(drawingView);
		});

		await AttachAndRun(otherPage, async _ =>
		{
			await drawingView.OnLoadedAsync();

			var handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
			Assert.True(handler.PlatformView.IsLoadedOnPlatform(), "The platform view of the DrawingView's handler is not in the window.");
			Assert.Same(((IElementHandler)otherLayout.Handler!).PlatformView, DrawingViewTestPlatform.GetParent(handler.PlatformView));
			Assert.Equal(2, handler.PlatformView.Lines.Count);
			Assert.Equal(blue, handler.PlatformView.Lines[^1].LineColor);

			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 80), (190, 80));
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[^1]);
			Assert.Equal(3, drawingView.Lines.Count);
		});
	}

	// Removing an item removes its DrawingView from the layout, which needs the DrawingView's handler. Changing the BindingContext of a DrawingView
	// stands for a recycling item template that reuses it for another item.
	[Fact]
	public async Task DrawingViewsOfAnItemTemplateWorkAfterTheItemsChangeWhileTheirPageIsAway()
	{
		SetupBuilder();
		var items = new ObservableCollection<ObservableCollection<IDrawingLine>>
		{
			new() { CreateLine(red, 4, (10, 20), (190, 20)) },
			new() { CreateLine(red, 4, (10, 30), (190, 30)) },
		};
		var layout = new VerticalStackLayout();
		BindableLayout.SetItemTemplate(layout, new DataTemplate(() =>
		{
			var itemDrawingView = CreateDrawingView();
			itemDrawingView.IsMultiLineModeEnabled = true;
			itemDrawingView.SetBinding(DrawingView.LinesProperty, Binding.SelfPath);
			return itemDrawingView;
		}));
		BindableLayout.SetItemsSource(layout, items);
		var page = new ContentPage { Content = layout };
		var removed = (DrawingView)layout[0];
		var reused = (DrawingView)layout[1];
		var removedUnloaded = new UnloadedRecorder(removed);
		var reusedUnloaded = new UnloadedRecorder(reused);
		var replacingItem = new ObservableCollection<IDrawingLine> { CreateLine(blue, 6, (10, 40), (190, 40)) };

		await AttachAndRun(page, async _ => await reused.OnLoadedAsync());
		await WaitUntilTheDrawingViewLeftTheWindowAsync(page, removed, removedUnloaded, unloadedBefore: 0);
		await WaitUntilTheDrawingViewLeftTheWindowAsync(page, reused, reusedUnloaded, unloadedBefore: 0);

		await InvokeOnMainThreadAsync(() =>
		{
			var exception = Record.Exception(() => items.RemoveAt(0));

			Assert.Null(exception);
			Assert.DoesNotContain<IView>(removed, layout);
			reused.BindingContext = replacingItem;
		});

		await AttachAndRun(page, async _ =>
		{
			await reused.OnLoadedAsync();

			var handler = Assert.IsType<DrawingViewHandler>(reused.Handler);
			AssertLine(Assert.Single(handler.PlatformView.Lines), blue, 6, (10, 40), (190, 40));
			var recorder = new DrawingViewRecorder(reused);
			DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 80), (190, 80));
			Assert.Same(Assert.Single(recorder.Completed), replacingItem[^1]);
			Assert.Equal(2, replacingItem.Count);
		});
	}

	[Fact]
	public async Task WaitingHandlerLeavesTheDrawingViewToAHandlerThatConnectedMeanwhile()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var drawingView = CreateDrawingView();
			var waitingHandler = CreateHandler<DrawingViewHandler>(drawingView);
			var oldPlatformView = waitingHandler.PlatformView;
			waitingHandler.StopListeningUntilLoaded();
			Assert.Same(waitingHandler, drawingView.Handler);

			var newHandler = new DrawingViewHandler();
			InitializeViewHandler(drawingView, newHandler); // sets DrawingView.Handler, as MAUI does when it creates a handler; MAUI then disconnects the waiting handler
			oldPlatformView.RaiseAttachedToWindow();
			waitingHandler.ListenAgain();

			Assert.Same(newHandler, drawingView.Handler);
			Assert.Same(drawingView, ((IElementHandler)newHandler).VirtualView);
			Assert.NotSame(oldPlatformView, newHandler.PlatformView);
			Assert.Null(((IElementHandler)waitingHandler).VirtualView);

			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(oldPlatformView, (10, 10), (50, 50));
			Assert.Empty(recorder.Started);
			Assert.Empty(drawingView.Lines);
			DrawingViewTestPlatform.Stroke(newHandler.PlatformView, (20, 20), (60, 60));
			Assert.Single(recorder.Completed);
			((IElementHandler)newHandler).DisconnectHandler();
		});
	}

	// AttachedToWindow is what the platform view raises on iOS and Mac Catalyst when it is in a window again; the test raises it itself, so it runs on every platform.
	[Fact]
	public async Task WaitingHandlerListensAgainWhenItsPlatformViewIsInAWindowAgain()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var drawingView = CreateDrawingView();
			drawingView.IsMultiLineModeEnabled = true;
			var handler = CreateHandler<DrawingViewHandler>(drawingView);
			var platformView = handler.PlatformView;
			handler.StopListeningUntilLoaded();
			drawingView.Lines.Add(CreateLine(red, 4, (10, 20), (190, 20)));
			Assert.Empty(platformView.Lines);

			var previousFactory = DrawingViewHandler.PlatformViewFactory;
			var platformViewsCreatedByTheFactory = 0;
			DrawingViewHandler.PlatformViewFactory = _ =>
			{
				platformViewsCreatedByTheFactory++;
				return DrawingViewTestPlatform.CreateView(200, 100);
			};
			try
			{
				platformView.RaiseAttachedToWindow();
			}
			finally
			{
				DrawingViewHandler.PlatformViewFactory = previousFactory;
			}

			Assert.Equal(0, platformViewsCreatedByTheFactory);
			Assert.Same(handler, drawingView.Handler);
			Assert.Same(platformView, handler.PlatformView);
			AssertLine(Assert.Single(platformView.Lines), red, 4, (10, 20), (190, 20));

			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(platformView, (10, 60), (50, 60));
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[^1]);
			drawingView.Lines.Clear();
			Assert.Empty(platformView.Lines);
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	// A Lines collection that replaces DrawingView.Lines while the handler waits is not observed, because an app that keeps that collection would keep the handler alive.
	[Fact]
	public async Task LinesReplacedWhileTheHandlerWaitsAreMappedWhenItListensAgain()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var drawingView = CreateDrawingView();
			drawingView.IsMultiLineModeEnabled = true;
			var handler = CreateHandler<DrawingViewHandler>(drawingView);
			var platformView = handler.PlatformView;
			handler.StopListeningUntilLoaded();

			var replacingLines = new ObservableCollection<IDrawingLine> { CreateLine(red, 4, (10, 20), (190, 20)) };
			drawingView.Lines = replacingLines;
			replacingLines.Add(CreateLine(blue, 6, (10, 40), (190, 40)));

			Assert.Same(handler, drawingView.Handler);
			Assert.Empty(platformView.Lines);

			platformView.RaiseAttachedToWindow();

			Assert.Equal(2, platformView.Lines.Count);
			AssertLine(platformView.Lines[0], red, 4, (10, 20), (190, 20));
			AssertLine(platformView.Lines[1], blue, 6, (10, 40), (190, 40));
			replacingLines.RemoveAt(0);
			AssertLine(Assert.Single(platformView.Lines), blue, 6, (10, 40), (190, 40));
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	// MAUI's HandlerDisconnectPolicy disconnects the handler of a page that is popped, also while the handler waits to be loaded again.
	[Fact]
	public async Task DisconnectingAWaitingHandlerStopsItWaiting()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var drawingView = CreateDrawingView();
			var handler = CreateHandler<DrawingViewHandler>(drawingView);
			var platformView = handler.PlatformView;
			handler.StopListeningUntilLoaded();

			((IElementHandler)handler).DisconnectHandler();
			platformView.RaiseAttachedToWindow();
			handler.ListenAgain();

			Assert.Null(drawingView.Handler);
			Assert.Null(((IElementHandler)handler).VirtualView);
			Assert.Null(((IElementHandler)handler).PlatformView);
			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));
			Assert.Empty(recorder.Started);
			Assert.Empty(drawingView.Lines);
		});
	}

	// Catches: ListenAgain subscribing to the Lines collections only through the Lines mapping.
	[Theory]
	[InlineData(ListenAgainSignal.Loaded)]
	[InlineData(ListenAgainSignal.AttachedToWindow)]
	public async Task HandlerWithoutTheLinesMappingSynchronizesTheLinesAgainWhenItListensAgain(ListenAgainSignal signal)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			Skip.If(signal is ListenAgainSignal.Loaded && loadedField is null, loadedFieldMissingReason);
			var drawingView = CreateDrawingView();
			drawingView.IsMultiLineModeEnabled = true;
			var mapper = new PropertyMapper<IDrawingView, DrawingViewHandler>
			{
				[nameof(IDrawingView.IsMultiLineModeEnabled)] = DrawingViewHandler.MapIsMultiLineModeEnabled
			};
			var handler = new DrawingViewHandler(mapper, null);
			InitializeViewHandler(drawingView, handler);
			var platformView = handler.PlatformView;
			DrawingViewTestPlatform.Stroke(platformView, (10, 10), (190, 10));
			var firstLine = Assert.Single(drawingView.Lines);

			RaiseUnloaded(drawingView);
			if (signal is ListenAgainSignal.Loaded)
			{
				RaiseLoaded(drawingView);
			}
			else
			{
				platformView.RaiseAttachedToWindow();
			}

			Assert.Same(handler, drawingView.Handler);
			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(platformView, (10, 60), (190, 60));
			Assert.Equal(2, platformView.Lines.Count);
			Assert.Equal(2, drawingView.Lines.Count);
			Assert.Same(firstLine, drawingView.Lines[0]);
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[1]);

			drawingView.Lines.Remove(firstLine);
			AssertPoint(10, 60, Assert.Single(platformView.Lines).Points[0]);
			((IElementHandler)handler).DisconnectHandler();
		});
	}

	// Catches: a second subscription to the platform view's Lines or to DrawingView.Lines when the handler listens again: a change of DrawingView.Lines that another
	// subscriber observes then threw InvalidOperationException, because the second subscription copied the platform lines back to DrawingView.Lines during its CollectionChanged event.
	[Fact]
	public Task HandlerThatListensAgainRelaysEachChangeOfTheLinesOnce()
	{
		var drawingView = CreateDrawingView();
		drawingView.IsMultiLineModeEnabled = true;

		return Run(drawingView, (handler, platformView) =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			Skip.If(loadedField is null, loadedFieldMissingReason);
			var changesBefore = DrawAndAddALine(10);
			drawingView.Lines.Clear();
			Assert.Empty(platformView.Lines); // precondition: the same lines before each measurement

			RaiseUnloaded(drawingView);
			RaiseLoaded(drawingView);

			Assert.Equal(changesBefore, DrawAndAddALine(60));

			(int StrokeChanges, int AddChanges) DrawAndAddALine(float y)
			{
				var drawingViewLinesChanges = new CollectionChangeCounter(drawingView.Lines);
				DrawingViewTestPlatform.Stroke(platformView, (10, y), (190, y));
				var strokeChanges = drawingViewLinesChanges.Count;

				var platformLinesChanges = new CollectionChangeCounter(platformView.Lines);
				drawingView.Lines.Add(CreateLine(red, 4, (10, y + 20), (190, y + 20)));
				Assert.Equal(2, platformView.Lines.Count);

				return (strokeChanges, platformLinesChanges.Count);
			}
		});
	}

	// MAUI sets DrawingView.Handler before ConnectHandler when a handler is given its DrawingView handler-first (IElementHandler.SetVirtualView), and raises
	// Unloaded in between (VisualElement.OnHandlerChangedCore) when the DrawingView was loaded and the new platform view is not in a window yet. The runner has
	// no MAUI Window, so Unloaded is raised from HandlerChanged, which MAUI raises in the same OnHandlerChangedCore call.
	[Fact]
	public async Task UnloadedBeforeTheHandlerConnectsMakesItWaitUntilTheDrawingViewIsLoaded()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			Skip.If(loadedField is null, loadedFieldMissingReason);
			var drawingView = CreateDrawingView();
			drawingView.IsMultiLineModeEnabled = true;
			var handler = new DrawingViewHandler();
			handler.SetMauiContext(MauiContext);
			drawingView.HandlerChanged += RaiseUnloadedOnHandlerChanged;
			handler.SetVirtualView(drawingView);
			drawingView.HandlerChanged -= RaiseUnloadedOnHandlerChanged;
			var platformView = handler.PlatformView;

			AssertHandlerStoppedListening(drawingView, handler, platformView);

			drawingView.Lines.Add(CreateLine(red, 4, (10, 20), (190, 20)));
			RaiseLoaded(drawingView);

			AssertLine(Assert.Single(platformView.Lines), red, 4, (10, 20), (190, 20));
			var recorder = new DrawingViewRecorder(drawingView);
			DrawingViewTestPlatform.Stroke(platformView, (10, 60), (190, 60));
			Assert.Single(recorder.Started);
			Assert.Same(Assert.Single(recorder.Completed), drawingView.Lines[^1]);
			Assert.Equal(2, drawingView.Lines.Count);
			((IElementHandler)handler).DisconnectHandler();

			void RaiseUnloadedOnHandlerChanged(object? sender, EventArgs e) => RaiseUnloaded(drawingView);
		});
	}

	[Fact]
	public async Task UnloadedDisconnectsAHandlerThatIsNotADrawingViewHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Skip.If(unloadedField is null, unloadedFieldMissingReason);
			var drawingView = CreateDrawingView();
			var handler = CreateHandler<OtherDrawingViewHandler>(drawingView);

			RaiseUnloaded(drawingView);

			Assert.Equal(1, handler.DisconnectCount);
			Assert.Null(drawingView.Handler);
		});
	}

	[Fact]
	public async Task HandlerViewAndPlatformViewDoNotLeak()
	{
		SetupBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new DrawingView();
			var handler = CreateHandler<DrawingViewHandler>(view);
			viewReference = new(view);
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference);
	}

	[Fact]
	public async Task UsedHandlerViewAndPlatformViewDoNotLeakAfterDisconnect()
	{
		SetupBuilder();
		WeakReference viewReference = null!, handlerReference = null!, platformViewReference = null!, recorderReference = null!;
		ObservableCollection<IDrawingLine> linesKeptByTheApp = null!;

		await InvokeOnMainThreadAsync(() =>
		{
			var view = new DrawingView { IsMultiLineModeEnabled = true };
			linesKeptByTheApp = view.Lines;
			var handler = CreateHandler<DrawingViewHandler>(view);
			var recorder = new DrawingViewRecorder(view);
			view.Lines.Add(CreateLine(red, 6, (10, 20), (90, 20)));
			DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 50), (50, 50));
			Assert.Equal(2, view.Lines.Count);
			viewReference = new(view);
			handlerReference = new(handler);
			platformViewReference = new(handler.PlatformView);
			recorderReference = new(recorder);
			((IElementHandler)handler).DisconnectHandler();
			view.Handler = null;
		});

		await AssertionExtensions.WaitForGC(viewReference, handlerReference, platformViewReference, recorderReference);
		GC.KeepAlive(linesKeptByTheApp);
	}

	[Fact]
	public async Task HostedAndUnloadedHandlerAndPlatformViewDoNotLeak()
	{
		SetupBuilder();
		WeakReference handlerReference = null!, platformViewReference = null!;

		await HostAndDropAsync();

		await DrawingViewTestPlatform.WaitUntilCollectedAsync(handlerReference, platformViewReference);

		async Task HostAndDropAsync()
		{
			var view = CreateDrawingView();
			await AttachAndRun<DrawingViewHandler>(view, async handler =>
			{
				await view.OnLoadedAsync();
				handlerReference = new(handler);
				platformViewReference = new(handler.PlatformView);
			});
			await view.OnUnloadedAsync();
		}
	}

	// The same page without a gesture is checked first as a control, so a failure of the second part is caused by the gesture (the Apple scroll-view cache), not by the hosting.
	[Fact]
	public async Task DrawingViewInAScrollViewIsCollectableAfterAGesture()
	{
		SetupBuilder();

		var control = await HostInAScrollViewAndDropAsync(drawOnIt: false);
		await DrawingViewTestPlatform.WaitUntilCollectedAsync(control);

		var drawnOn = await HostInAScrollViewAndDropAsync(drawOnIt: true);
		await DrawingViewTestPlatform.WaitUntilCollectedAsync(drawnOn);

		async Task<WeakReference[]> HostInAScrollViewAndDropAsync(bool drawOnIt)
		{
			WeakReference[] references = [];
			var drawingView = CreateDrawingView();
			var page = new ContentPage { Content = new ScrollView { Content = new VerticalStackLayout { drawingView } } };

			await AttachAndRun(page, async _ =>
			{
				await drawingView.OnLoadedAsync();
				var handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
				if (drawOnIt)
				{
					DrawingViewTestPlatform.Stroke(handler.PlatformView, (20, 20), (60, 60));
					Assert.Single(drawingView.Lines); // precondition: the gesture reached the DrawingView
				}

				references = [new(handler), new(handler.PlatformView), new(drawingView)];
			});

			return references;
		}
	}

	[Fact]
	public async Task DrawingViewWaitingToBeLoadedAgainDoesNotLeak()
	{
		SetupBuilder();

		var dropped = await HostLeaveAndDropAsync();
		await DrawingViewTestPlatform.WaitUntilCollectedAsync(dropped);

		async Task<WeakReference[]> HostLeaveAndDropAsync()
		{
			WeakReference[] references = [];
			var drawingView = CreateDrawingView();
			var unloaded = new UnloadedRecorder(drawingView);
			var page = new ContentPage { Content = new VerticalStackLayout { drawingView } };

			await AttachAndRun(page, async _ =>
			{
				await drawingView.OnLoadedAsync();
				var handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
				references = [new(handler), new(handler.PlatformView), new(drawingView)];
			});
			await WaitUntilTheDrawingViewLeftTheWindowAsync(page, drawingView, unloaded, unloadedBefore: 0);
			await InvokeOnMainThreadAsync(() => Assert.Same(references[0].Target, drawingView.Handler)); // precondition: kept, waiting to be loaded again

			return references;
		}
	}

	// The leak that #420 fixed: the app keeps the DrawingView's Lines (a view model's collection, which the handler observed while the page was shown).
	// Catches: the handler still observing the app's Lines, or the platform view still holding the handler, after the DrawingView was unloaded.
	[Fact]
	public async Task DrawingViewWhosePageNeverReturnsIsCollectedWhileTheAppKeepsItsLines()
	{
		SetupBuilder();
		var linesKeptByTheApp = new ObservableCollection<IDrawingLine>();

		var dropped = await HostLeaveAndDropAsync();
		await DrawingViewTestPlatform.WaitUntilCollectedAsync(dropped);
		Assert.Single(linesKeptByTheApp);

		async Task<WeakReference[]> HostLeaveAndDropAsync()
		{
			WeakReference[] references = [];
			var drawingView = CreateDrawingView();
			drawingView.IsMultiLineModeEnabled = true;
			drawingView.Lines = linesKeptByTheApp;
			var unloaded = new UnloadedRecorder(drawingView);
			var page = new ContentPage { Content = new VerticalStackLayout { drawingView } };

			await AttachAndRun(page, async _ =>
			{
				await drawingView.OnLoadedAsync();
				var handler = Assert.IsType<DrawingViewHandler>(drawingView.Handler);
				DrawingViewTestPlatform.Stroke(handler.PlatformView, (10, 10), (190, 10));
				Assert.Single(linesKeptByTheApp); // precondition: the handler copies the drawn line to the app's collection
				references = [new(handler), new(handler.PlatformView), new(drawingView), new(page)];
			});
			await WaitUntilTheDrawingViewLeftTheWindowAsync(page, drawingView, unloaded, unloadedBefore: 0);
			await InvokeOnMainThreadAsync(() => Assert.Same(references[0].Target, drawingView.Handler)); // precondition: kept while its page is away

			return references;
		}
	}

	[Fact]
	public Task GetImageStreamRendersTheLinesDrawnOnTheHostedView()
	{
		var drawingView = CreateDrawingView();
		drawingView.Background = new SolidColorBrush(white);

		return RunAsync(drawingView, async (handler, platformView) =>
		{
			drawingView.LineColor = red;
			drawingView.LineWidth = 6;
			DrawingViewTestPlatform.Stroke(platformView, (20, 50), (180, 50));

			using var canvasStream = await drawingView.GetImageStream(200, 100, DrawingViewOutputOption.FullCanvas);
			var canvasImage = await DecodeAsync(canvasStream);
			DrawingPixelAssert.SizeIs(canvasImage, 200, 100);
			DrawingPixelAssert.NotColorAtUnits(canvasImage, 100, 50, white, because: "the line is at its view coordinates in the canvas image");
			DrawingPixelAssert.ColorAtUnits(canvasImage, 100, 85, white);

			// lines only: the bounding box of (20,50)-(180,50) padded by the width 6 = 172 x 12
			using var linesStream = await drawingView.GetImageStream(172, 12, DrawingViewOutputOption.Lines);
			var linesImage = await DecodeAsync(linesStream);
			DrawingPixelAssert.SizeIs(linesImage, 172, 12);
			DrawingPixelAssert.NotColorAtUnits(linesImage, 86, 6, white);

			using var defaultStream = await drawingView.GetImageStream(172, 12);
			DrawingPixelAssert.SameImage(linesImage, await DecodeAsync(defaultStream));
			using var staticStream = await DrawingView.GetImageStream(ImageLineOptions.JustLines(drawingView.Lines.ToList(), new Size(172, 12), new SolidPaint(white)));
			DrawingPixelAssert.SameImage(linesImage, await DecodeAsync(staticStream));
		});
	}

	[Theory]
	[InlineData(DrawingViewOutputOption.Lines)]
	[InlineData(DrawingViewOutputOption.FullCanvas)]
	public Task GetImageStreamOfADrawingViewWithoutLinesReturnsNullStream(DrawingViewOutputOption outputOption)
	{
		var drawingView = CreateDrawingView();
		drawingView.Background = new SolidColorBrush(white);

		return RunAsync(drawingView, async (_, _) =>
		{
			Assert.Empty(drawingView.Lines);
			Assert.True(drawingView.Width >= 1 && drawingView.Height >= 1, "Precondition: the DrawingView is laid out, so a FullCanvas export has a canvas of at least one pixel.");

			using var stream = await drawingView.GetImageStream(200, 100, outputOption);

			Assert.Same(Stream.Null, stream);
		});
	}

	DrawingView CreateDrawingView() => new() { WidthRequest = DrawingViewTestPlatform.DefaultWidth, HeightRequest = DrawingViewTestPlatform.DefaultHeight };

	static DrawingLine CreateLine(Color color, float width, params (float X, float Y)[] points) => new()
	{
		LineColor = color,
		LineWidth = width,
		ShouldSmoothPathWhenDrawn = false,
		Points = new ObservableCollection<PointF>(points.Select(point => new PointF(point.X, point.Y)))
	};

	async Task RunAsync(DrawingView drawingView, Func<DrawingViewHandler, MauiDrawingView, Task> body)
	{
		SetupBuilder();

		await AttachAndRun<DrawingViewHandler>(drawingView, async handler =>
		{
			await drawingView.OnLoadedAsync();
#if WINDOWS
			// A view that AttachAndRun hosts on its own is laid out by InitializeViewHandler before it is hosted, but on Windows a view that is not loaded cannot be measured,
			// so that arrange leaves an empty Frame (centered, 0x0) and no MAUI parent lays it out later.
			var size = ((IView)drawingView).Measure(drawingView.WidthRequest, drawingView.HeightRequest);
			((IView)drawingView).Arrange(new Rect(0, 0, size.Width, size.Height));
#endif
			if (!await AssertHelpers.Wait(() => drawingView.Width > 0 && drawingView.Height > 0, timeout: 2000))
			{
				throw new Xunit.Sdk.XunitException($"The DrawingView was never laid out (Frame {drawingView.Frame}, DesiredSize {drawingView.DesiredSize}, requested {drawingView.WidthRequest}x{drawingView.HeightRequest}).");
			}
			await body(handler, handler.PlatformView);
		});
	}

	Task Run(DrawingView drawingView, Action<DrawingViewHandler, MauiDrawingView> body) =>
		RunAsync(drawingView, (handler, platformView) =>
		{
			body(handler, platformView);
			return Task.CompletedTask;
		});

	Task<DrawingPixelImage> RenderAsync(MauiDrawingView platformView) => DrawingViewTestPlatform.RenderAsync(platformView, MauiContext);

	// UI thread only.
	static void RaiseUnloaded(VisualElement element)
	{
		var field = unloadedField ?? throw new InvalidOperationException(unloadedFieldMissingReason);
		((EventHandler?)field.GetValue(element))?.Invoke(element, EventArgs.Empty);
	}

	// UI thread only.
	static void RaiseLoaded(VisualElement element)
	{
		var field = loadedField ?? throw new InvalidOperationException(loadedFieldMissingReason);
		((EventHandler?)field.GetValue(element))?.Invoke(element, EventArgs.Empty);
	}

	// UI thread only.
	static bool IsListeningToUnloaded(DrawingView drawingView) =>
		((Delegate?)unloadedField?.GetValue(drawingView))?.GetInvocationList().Any(subscriber => ReferenceEquals(subscriber.Target, drawingView)) is true;

	// UI thread only.
	static void AssertHandlerStoppedListening(DrawingView drawingView, DrawingViewHandler handler, MauiDrawingView platformView)
	{
		Assert.Same(handler, drawingView.Handler);
		Assert.Same(platformView, handler.PlatformView);

		var recorder = new DrawingViewRecorder(drawingView);
		var lines = drawingView.Lines.ToList();
		DrawingViewTestPlatform.Stroke(platformView, (10, 10), (50, 50));

		Assert.Empty(recorder.Started);
		Assert.Empty(recorder.Points);
		Assert.Empty(recorder.Completed);
		Assert.Equal(lines, drawingView.Lines);

		var platformLines = platformView.Lines.ToList();
		var line = CreateLine(red, 4, (10, 20), (90, 20));
		drawingView.Lines.Add(line);
		Assert.Equal(platformLines, platformView.Lines);
		drawingView.Lines.Remove(line);
	}

	// The headless iOS and Mac Catalyst host has no MAUI Window and does not raise Unloaded for the DrawingView, so there it is raised the way MAUI does.
	static async Task WaitUntilTheDrawingViewLeftTheWindowAsync(ContentPage page, DrawingView drawingView, UnloadedRecorder unloaded, int unloadedBefore)
	{
		await page.OnUnloadedAsync();
		if (unloaded.Count == unloadedBefore)
		{
			Skip.If(unloadedField is null, unloadedNotRaisedReason);
			await InvokeOnMainThreadAsync(() => RaiseUnloaded(drawingView));
		}

		Assert.NotEqual(unloadedBefore, unloaded.Count);
	}

	static async Task<DrawingPixelImage> DecodeAsync(Stream stream)
	{
		Assert.NotSame(Stream.Null, stream);
		stream.Position = 0;
		return await DrawingViewTestPlatform.DecodePngAsync(stream);
	}

	static void AssertPoint(double expectedX, double expectedY, PointF actual)
	{
		Assert.InRange(actual.X, expectedX - 0.01, expectedX + 0.01);
		Assert.InRange(actual.Y, expectedY - 0.01, expectedY + 0.01);
	}

	static void AssertLine(MauiDrawingLine line, Color color, float width, params (float X, float Y)[] points)
	{
		Assert.Equal(color, line.LineColor);
		Assert.Equal(width, line.LineWidth);
		Assert.Equal(points.Length, line.Points.Count);
		for (var i = 0; i < points.Length; i++)
		{
			AssertPoint(points[i].X, points[i].Y, line.Points[i]);
		}
	}

	public enum ListenAgainSignal
	{
		Loaded,
		AttachedToWindow
	}

	// Plain instance methods as handlers: the DrawingView keeps its subscribers in a WeakEventManager.
	sealed class DrawingViewRecorder
	{
		public DrawingViewRecorder(DrawingView view)
		{
			view.DrawingLineStarted += OnStarted;
			view.PointDrawn += OnPointDrawn;
			view.DrawingLineCompleted += OnCompleted;
			view.DrawingLineCancelled += OnCancelled;
		}

		public List<PointF> Started { get; } = [];

		public List<PointF> Points { get; } = [];

		public List<IDrawingLine> Completed { get; } = [];

		public int Cancelled { get; private set; }

		void OnStarted(object? sender, DrawingLineStartedEventArgs e) => Started.Add(e.Point);

		void OnPointDrawn(object? sender, PointDrawnEventArgs e) => Points.Add(e.Point);

		void OnCompleted(object? sender, DrawingLineCompletedEventArgs e) => Completed.Add(e.LastDrawingLine);

		void OnCancelled(object? sender, EventArgs e) => Cancelled++;
	}

	sealed class CollectionChangeCounter
	{
		public CollectionChangeCounter(INotifyCollectionChanged collection) => collection.CollectionChanged += OnCollectionChanged;

		public int Count { get; private set; }

		void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Count++;
	}

	sealed class OtherDrawingViewHandler : ViewHandler<IDrawingView, MauiDrawingView>
	{
		public OtherDrawingViewHandler() : base(ViewHandler.ViewMapper)
		{
		}

		public int DisconnectCount { get; private set; }

		protected override MauiDrawingView CreatePlatformView() => DrawingViewTestPlatform.CreateView(200, 100);

		protected override void DisconnectHandler(MauiDrawingView platformView)
		{
			DisconnectCount++;
			base.DisconnectHandler(platformView);
		}
	}

	sealed class UnloadedRecorder
	{
		public UnloadedRecorder(DrawingView view) => view.Unloaded += OnUnloaded;

		public int Count { get; private set; }

		void OnUnloaded(object? sender, EventArgs e) => Count++;
	}

	sealed class DrawCounter
	{
		int count;

		public int Count => Volatile.Read(ref count);

		public void Draw(ICanvas canvas, RectF dirtyRect) => Interlocked.Increment(ref count);
	}

	sealed class TaggedDrawingLine : DrawingLine;

	sealed class TaggingAdapter : IDrawingLineAdapter
	{
		public List<MauiDrawingLine> Converted { get; } = [];

		public IDrawingLine ConvertMauiDrawingLine(MauiDrawingLine mauiDrawingLine)
		{
			Converted.Add(mauiDrawingLine);
			return new TaggedDrawingLine
			{
				LineColor = mauiDrawingLine.LineColor,
				LineWidth = mauiDrawingLine.LineWidth,
				Granularity = mauiDrawingLine.Granularity,
				ShouldSmoothPathWhenDrawn = mauiDrawingLine.ShouldSmoothPathWhenDrawn,
				Points = new ObservableCollection<PointF>(mauiDrawingLine.Points)
			};
		}
	}
}