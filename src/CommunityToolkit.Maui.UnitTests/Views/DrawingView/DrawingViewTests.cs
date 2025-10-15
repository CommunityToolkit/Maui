using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

[Collection(nameof(DrawingViewTests)), CollectionDefinition(nameof(DrawingViewTests), DisableParallelization = true)]
public class DrawingViewTests(ITestOutputHelper testOutputHelper) : BaseViewTest
{
	[Fact]
	public void DrawingViewShouldBeAssignedToIDrawingView()
	{
		new DrawingView().Should().BeAssignableTo<IDrawingView>();
	}

	[Fact]
	public void GetRequiredServiceThrowsOnNoContext()
	{
		var handlerStub = new MockDrawingViewHandler();

		(handlerStub as IElementHandler).MauiContext.Should().BeNull();

		var ex = Assert.Throws<InvalidOperationException>(() => handlerStub.GetRequiredService<IDrawingView>());

		ex.Message.Should().Be("Unable to find the context. The MauiContext property should have been set by the host.");
	}

	[Fact]
	public void DefaultLinesShouldNotBeShared()
	{
		new DrawingView().Lines.Should().NotBeSameAs(new DrawingView().Lines);
	}

	[Fact]
	public void OnLinesCollectionChangedHandlerIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingView.Lines.Add(new DrawingLine());
		drawingViewHandler.Lines.Should().HaveCount(1);

		drawingView.Lines.Add(new DrawingLine());
		drawingViewHandler.Lines.Should().HaveCount(2);
	}

	[Fact]
	public void LineWidthMapperIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapLineWidthCount.Should().Be(1);

		drawingView.LineWidth = 1;
		drawingViewHandler.MapLineWidthCount.Should().Be(2);
	}

	[Fact]
	public void ClearOnFinishMapperIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapShouldSmoothPathWhenDrawnCount.Should().Be(1);

		drawingView.ShouldClearOnFinish = true;
		drawingViewHandler.MapShouldSmoothPathWhenDrawnCount.Should().Be(2);
	}

	[Fact]
	public void LineColorMapperIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapLineColorCount.Should().Be(1);

		drawingView.LineColor = Colors.Blue;
		drawingViewHandler.MapLineColorCount.Should().Be(2);
	}

	[Fact]
	public void MultiLineModeMapperIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapIsMultiLineModeEnabledCount.Should().Be(1);

		drawingView.IsMultiLineModeEnabled = true;
		drawingViewHandler.MapIsMultiLineModeEnabledCount.Should().Be(2);
	}


	[Fact]
	public void DrawMapperIsCalled()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapDrawCount.Should().Be(1);

		drawingView.DrawAction = (_, _) => testOutputHelper.WriteLine("DrawActionCalled");
		drawingViewHandler.MapDrawCount.Should().Be(2);
	}

	[Fact]
	public void VerifyDefaultValues()
	{
		var drawingView = new DrawingView();

		Assert.Multiple(() =>
		{
			drawingView.LineWidth.Should().Be(DrawingViewDefaults.LineWidth);
			drawingView.IsMultiLineModeEnabled.Should().Be(DrawingViewDefaults.IsMultiLineModeEnabled);
			drawingView.ShouldClearOnFinish.Should().Be(DrawingViewDefaults.ShouldClearOnFinish);
			drawingView.LineColor.Should().Be(DrawingViewDefaults.LineColor);
			drawingView.BackgroundColor.Should().Be(DrawingViewDefaults.BackgroundColor);
		});
	}

	[Fact]
	public void ClearShouldClearLines()
	{
		var drawingView = new DrawingView
		{
			Lines = [new DrawingLine()]
		};

		drawingView.Lines.Should().HaveCount(1);

		drawingView.Clear();

		drawingView.Lines.Should().BeEmpty();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await Task.Delay(100, TestContext.Current.CancellationToken);

		ImageLineOptions options = ImageLineOptions.JustLines([new DrawingLine()], Size.Zero, Colors.Transparent.AsPaint());

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await DrawingView.GetImageStream(options, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenCanceled()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await cts.CancelAsync();

		ImageLineOptions options = ImageLineOptions.JustLines([new DrawingLine()], Size.Zero, Colors.Transparent.AsPaint());

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await DrawingView.GetImageStream(options, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamReturnsNullStream()
	{
		var drawingView = new DrawingView();

		var stream = await drawingView.GetImageStream(10, 10, TestContext.Current.CancellationToken);
		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamStaticReturnsNullStream()
	{
		ImageLineOptions options = ImageLineOptions.JustLines([new DrawingLine()], Size.Zero, Colors.Blue.AsPaint());
		var stream = await DrawingView.GetImageStream(options, TestContext.Current.CancellationToken);
		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact]
	public void DefaultDrawingLineAdapter_IDrawingLineIsDrawingLine()
	{
		var drawingView = new DrawingView();

		CreateViewHandler<MockDrawingViewHandler>(drawingView);
		IDrawingLine? currentLine = null;
		var action = new EventHandler<DrawingLineCompletedEventArgs>((_, e) => currentLine = e.LastDrawingLine);
		drawingView.DrawingLineCompleted += action;

		drawingView.Lines.Add(new DrawingLine
		{
			LineColor = Colors.BlanchedAlmond
		});

		drawingView.DrawingLineCompleted -= action;

		Assert.NotNull(currentLine);
		currentLine?.Should().BeOfType<DrawingLine>();
		currentLine?.LineColor.Should().Be(Colors.BlanchedAlmond);
	}

	[Fact]
	public void SetDefaultDrawingLineAdapter_IDrawingLineIsDrawingLine()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingViewHandler.SetDrawingLineAdapter(new DrawingLineAdapter());

		IDrawingLine? currentLine = null;
		var action = new EventHandler<DrawingLineCompletedEventArgs>((_, e) => currentLine = e.LastDrawingLine);

		drawingView.DrawingLineCompleted += action;

		drawingView.Lines.Add(new DrawingLine
		{
			LineColor = Colors.Linen
		});

		drawingView.DrawingLineCompleted -= action;

		currentLine.Should().NotBeNull();
		currentLine.Should().BeOfType<DrawingLine>();
		currentLine?.LineColor.Should().Be(Colors.Linen);
	}

	[Fact]
	public void SetDrawingLineAdapter_IDrawingLineIsMockDrawingLine()
	{
		var drawingView = new DrawingView();

		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingViewHandler.SetDrawingLineAdapter(new MockDrawingLineAdapter());

		IDrawingLine? currentLine = null;
		var action = new EventHandler<DrawingLineCompletedEventArgs>((_, e) => currentLine = e.LastDrawingLine);

		drawingView.DrawingLineCompleted += action;

		drawingView.Lines.Add(new DrawingLine
		{
			LineColor = Colors.LimeGreen
		});

		drawingView.DrawingLineCompleted -= action;

		currentLine.Should().NotBeNull();
		currentLine.Should().BeOfType<MockDrawingLine>();
		currentLine?.LineColor.Should().Be(Colors.LimeGreen);
	}

	[Fact]
	public void OnDrawingStartedLastPointPassedWithCommand()
	{
		var drawingView = new DrawingView();

		var expectedPoint = new PointF(10, 10);

		PointF? point = null;
		drawingView.DrawingLineStartedCommand = new Command<PointF>(p => point = p);
		((IDrawingView)drawingView).OnDrawingLineStarted(expectedPoint);

		point.Should().BeEquivalentTo(expectedPoint);
	}

	[Fact]
	public void OnDrawingLastPointPassedWithCommand()
	{
		var drawingView = new DrawingView();

		var expectedPoint = new PointF(10, 10);

		PointF? point = null;
		drawingView.PointDrawnCommand = new Command<PointF>(p => point = p);
		((IDrawingView)drawingView).OnPointDrawn(expectedPoint);

		point.Should().BeEquivalentTo(expectedPoint);
	}

	[Fact]
	public void OnDrawingLineCompletedLastDrawingLinePassedWithCommand()
	{
		var drawingView = new DrawingView();

		var expectedDrawingLine = new DrawingLine
		{
			LineColor = Colors.Grey,
			LineWidth = 11f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 21,
			Points = [new PointF(10, 10)]
		};

		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(line => currentLine = line);
		((IDrawingView)drawingView).OnDrawingLineCompleted(expectedDrawingLine);

		currentLine.Should().BeEquivalentTo(expectedDrawingLine);
	}

	[Fact]
	public void OnDrawingLineCompleted_CommandIsNull_LastDrawingLineNotPassed()
	{
		var drawingView = new DrawingView();

		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = null;
		((IDrawingView)drawingView).OnDrawingLineCompleted(new DrawingLine());

		currentLine.Should().BeNull();
	}

	[Fact]
	public void OnDrawingStarted_CommandIsNull_LastDrawingPointNotPassed()
	{
		var drawingView = new DrawingView();

		PointF? currentPoint = null;
		drawingView.DrawingLineStartedCommand = null;
		((IDrawingView)drawingView).OnDrawingLineStarted(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawing_CommandIsNull_LastDrawingPointNotPassed()
	{
		var drawingView = new DrawingView();

		PointF? currentPoint = null;
		drawingView.PointDrawnCommand = null;
		((IDrawingView)drawingView).OnPointDrawn(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawingLineCompleted_CommandIsNotAllowedExecute_LastDrawingLineNotPassed()
	{
		var drawingView = new DrawingView();

		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(line => currentLine = line, _ => false);
		((IDrawingView)drawingView).OnDrawingLineCompleted(new DrawingLine());

		currentLine.Should().BeNull();
	}

	[Fact]
	public void OnDrawingStarted_CommandIsNotAllowedExecute_LastDrawingPointNotPassed()
	{
		var drawingView = new DrawingView();

		PointF? currentPoint = null;
		drawingView.DrawingLineStartedCommand = new Command<PointF>(p => currentPoint = p, _ => false);
		((IDrawingView)drawingView).OnDrawingLineStarted(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawing_CommandIsNotAllowedExecute_LastDrawingPointNotPassed()
	{
		var drawingView = new DrawingView();

		PointF? currentPoint = null;
		drawingView.PointDrawnCommand = new Command<PointF>(p => currentPoint = p, _ => false);
		((IDrawingView)drawingView).OnPointDrawn(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawingLineCompletedLastDrawingLinePassedWithEvent()
	{
		var drawingView = new DrawingView();

		var expectedDrawingLine = new DrawingLine
		{
			LineColor = Colors.GreenYellow,
			LineWidth = 15f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 55,
			Points = [new PointF(10, 10)]
		};

		IDrawingLine? currentLine = null;
		var action = new EventHandler<DrawingLineCompletedEventArgs>((_, e) => currentLine = e.LastDrawingLine);
		drawingView.DrawingLineCompleted += action;
		((IDrawingView)drawingView).OnDrawingLineCompleted(expectedDrawingLine);
		drawingView.DrawingLineCompleted -= action;

		currentLine.Should().BeEquivalentTo(expectedDrawingLine);
	}

	[Fact]
	public void OnDrawingStartedLastDrawingPointPassedWithEvent()
	{
		var drawingView = new DrawingView();

		var expectedPoint = new PointF(10, 10);

		PointF? currentPoint = null;
		var action = new EventHandler<DrawingLineStartedEventArgs>((_, e) => currentPoint = e.Point);
		drawingView.DrawingLineStarted += action;
		((IDrawingView)drawingView).OnDrawingLineStarted(expectedPoint);
		drawingView.DrawingLineStarted -= action;

		currentPoint.Should().BeEquivalentTo(expectedPoint);
	}

	[Fact]
	public void OnDrawingLastDrawingPointPassedWithEvent()
	{
		var drawingView = new DrawingView();

		var expectedPoint = new PointF(10, 10);

		PointF? currentPoint = null;
		var action = new EventHandler<PointDrawnEventArgs>((_, e) => currentPoint = e.Point);
		drawingView.PointDrawn += action;
		((IDrawingView)drawingView).OnPointDrawn(expectedPoint);
		drawingView.PointDrawn -= action;

		currentPoint.Should().BeEquivalentTo(expectedPoint);
	}
}