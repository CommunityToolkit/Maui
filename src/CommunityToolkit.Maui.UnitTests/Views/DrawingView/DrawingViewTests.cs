using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.UnitTests.Mocks;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingViewTests : BaseHandlerTest
{
	readonly ITestOutputHelper testOutputHelper;
	readonly Maui.Views.DrawingView drawingView = new();

	public DrawingViewTests(ITestOutputHelper testOutputHelper)
	{
		this.testOutputHelper = testOutputHelper;
	}

	[Fact]
	public void DrawingViewShouldBeAssignedToIDrawingView()
	{
		new Maui.Views.DrawingView().Should().BeAssignableTo<IDrawingView>();
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
		new Maui.Views.DrawingView().Lines.Should().NotBeSameAs(new Maui.Views.DrawingView().Lines);
	}

	[Fact]
	public void OnLinesCollectionChangedHandlerIsCalled()
	{
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
		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapLineWidthCount.Should().Be(1);

		drawingView.LineWidth = 1;
		drawingViewHandler.MapLineWidthCount.Should().Be(2);
	}

	[Fact]
	public void ClearOnFinishMapperIsCalled()
	{
		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapShouldSmoothPathWhenDrawnCount.Should().Be(1);

		drawingView.ShouldClearOnFinish = true;
		drawingViewHandler.MapShouldSmoothPathWhenDrawnCount.Should().Be(2);
	}

	[Fact]
	public void LineColorMapperIsCalled()
	{
		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapLineColorCount.Should().Be(1);

		drawingView.LineColor = Colors.Blue;
		drawingViewHandler.MapLineColorCount.Should().Be(2);
	}

	[Fact]
	public void MultiLineModeMapperIsCalled()
	{
		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapIsMultiLineModeEnabledCount.Should().Be(1);

		drawingView.IsMultiLineModeEnabled = true;
		drawingViewHandler.MapIsMultiLineModeEnabledCount.Should().Be(2);
	}


	[Fact]
	public void DrawMapperIsCalled()
	{
		var drawingViewHandler = CreateViewHandler<MockDrawingViewHandler>(drawingView);
		drawingView.Handler.Should().NotBeNull();

		drawingViewHandler.MapDrawCount.Should().Be(1);

		drawingView.DrawAction = (_, _) => testOutputHelper.WriteLine("DrawActionCalled");
		drawingViewHandler.MapDrawCount.Should().Be(2);
	}

	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new Maui.Views.DrawingView
		{
			LineColor = DrawingViewDefaults.LineColor,
			LineWidth = DrawingViewDefaults.LineWidth,
			IsMultiLineModeEnabled = DrawingViewDefaults.IsMultiLineModeEnabled,
			ShouldClearOnFinish = DrawingViewDefaults.ShouldClearOnFinish,
			Lines = [],
			DrawAction = null,
			DrawingLineCompletedCommand = null,
		};

		drawingView.Should().BeEquivalentTo(expectedDefaultValue, config => config.Excluding(ctx => ctx.Id));
	}

	[Fact]
	public void ClearShouldClearLines()
	{
		drawingView.Lines = [new DrawingLine()];
		drawingView.Lines.Should().HaveCount(1);

		drawingView.Clear();

		drawingView.Lines.Should().BeEmpty();
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await Maui.Views.DrawingView.GetImageStream(new[] { new DrawingLine() }, Size.Zero, Colors.Blue, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenCanceled()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await Maui.Views.DrawingView.GetImageStream(new[] { new DrawingLine() }, Size.Zero, Colors.Blue, cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamReturnsNullStream()
	{
		var stream = await drawingView.GetImageStream(10, 10, CancellationToken.None);
		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamStaticReturnsNullStream()
	{
		var stream = await Maui.Views.DrawingView.GetImageStream(new[] { new DrawingLine() }, Size.Zero, Colors.Blue, CancellationToken.None);
		stream.Should().BeSameAs(Stream.Null);
	}

	[Fact]
	public void DefaultDrawingLineAdapter_IDrawingLineIsDrawingLine()
	{
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
		var expectedPoint = new PointF(10, 10);

		PointF? point = null;
		drawingView.DrawingLineStartedCommand = new Command<PointF>(p => point = p);
		((IDrawingView)drawingView).OnDrawingLineStarted(expectedPoint);

		point.Should().BeEquivalentTo(expectedPoint);
	}

	[Fact]
	public void OnDrawingLastPointPassedWithCommand()
	{
		var expectedPoint = new PointF(10, 10);

		PointF? point = null;
		drawingView.PointDrawnCommand = new Command<PointF>(p => point = p);
		((IDrawingView)drawingView).OnPointDrawn(expectedPoint);

		point.Should().BeEquivalentTo(expectedPoint);
	}

	[Fact]
	[Obsolete]
	public void OnDrawingLineCompletedLastDrawingLinePassedWithCommand()
	{
		var expectedDrawingLine = new DrawingLine
		{
			LineColor = Colors.Grey,
			LineWidth = 11f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 21,
			Points = new ObservableCollection<PointF>(new[] { new PointF(10, 10) })
		};

		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(line => currentLine = line);
		((IDrawingView)drawingView).OnDrawingLineCompleted(expectedDrawingLine);

		currentLine.Should().BeEquivalentTo(expectedDrawingLine);
	}

	[Fact]
	[Obsolete]
	public void OnDrawingLineCompleted_CommandIsNull_LastDrawingLineNotPassed()
	{
		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = null;
		((IDrawingView)drawingView).OnDrawingLineCompleted(new DrawingLine());

		currentLine.Should().BeNull();
	}

	[Fact]
	public void OnDrawingStarted_CommandIsNull_LastDrawingPointNotPassed()
	{
		PointF? currentPoint = null;
		drawingView.DrawingLineStartedCommand = null;
		((IDrawingView)drawingView).OnDrawingLineStarted(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawing_CommandIsNull_LastDrawingPointNotPassed()
	{
		PointF? currentPoint = null;
		drawingView.PointDrawnCommand = null;
		((IDrawingView)drawingView).OnPointDrawn(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	[Obsolete]
	public void OnDrawingLineCompleted_CommandIsNotAllowedExecute_LastDrawingLineNotPassed()
	{
		IDrawingLine? currentLine = null;
		drawingView.DrawingLineCompletedCommand = new Command<IDrawingLine>(line => currentLine = line, _ => false);
		((IDrawingView)drawingView).OnDrawingLineCompleted(new DrawingLine());

		currentLine.Should().BeNull();
	}

	[Fact]
	public void OnDrawingStarted_CommandIsNotAllowedExecute_LastDrawingPointNotPassed()
	{
		PointF? currentPoint = null;
		drawingView.DrawingLineStartedCommand = new Command<PointF>(p => currentPoint = p, _ => false);
		((IDrawingView)drawingView).OnDrawingLineStarted(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	public void OnDrawing_CommandIsNotAllowedExecute_LastDrawingPointNotPassed()
	{
		PointF? currentPoint = null;
		drawingView.PointDrawnCommand = new Command<PointF>(p => currentPoint = p, _ => false);
		((IDrawingView)drawingView).OnPointDrawn(new PointF());

		currentPoint.Should().BeNull();
	}

	[Fact]
	[Obsolete]
	public void OnDrawingLineCompletedLastDrawingLinePassedWithEvent()
	{
		var expectedDrawingLine = new DrawingLine
		{
			LineColor = Colors.GreenYellow,
			LineWidth = 15f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 55,
			Points = new ObservableCollection<PointF>(new[] { new PointF(10, 10) })
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
		var expectedPoint = new PointF(10, 10);

		PointF? currentPoint = null;
		var action = new EventHandler<PointDrawnEventArgs>((_, e) => currentPoint = e.Point);
		drawingView.PointDrawn += action;
		((IDrawingView)drawingView).OnPointDrawn(expectedPoint);
		drawingView.PointDrawn -= action;

		currentPoint.Should().BeEquivalentTo(expectedPoint);
	}
}