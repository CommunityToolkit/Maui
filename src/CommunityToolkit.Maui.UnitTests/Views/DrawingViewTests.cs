using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingViewTests : BaseHandlerTest
{
	readonly IDrawingView drawingView = new DrawingView();

	public DrawingViewTests()
	{
		Assert.IsAssignableFrom<IDrawingView>(new DrawingView());
	}

	[Fact]
	public void GetRequiredServiceThrowsOnNoContext()
	{
		var handlerStub = new MockDrawingViewHandler();

		Assert.Null((handlerStub as IElementHandler).MauiContext);

		var ex = Assert.Throws<InvalidOperationException>(() => handlerStub.GetRequiredService<IDrawingView>());

		Assert.Contains("the context", ex.Message);
		Assert.Contains("MauiContext", ex.Message);
	}

	[Fact]
	public void OnLinesCollectionChangedHandlerIsCalled()
	{
		var drawingViewHandler = CreateElementHandler<MockDrawingViewHandler>(drawingView);

		Assert.NotNull(drawingView.Handler);

		drawingView.Lines.Add(new DrawingLine());
		Assert.Single(drawingViewHandler.Lines);

		drawingView.Lines.Add(new DrawingLine());
		Assert.Equal(2, drawingViewHandler.Lines.Count);
	}

	[Fact]
	public void LineWidthMapperIsCalled()
	{
		var drawingViewHandler = CreateElementHandler<MockDrawingViewHandler>(drawingView);
		Assert.NotNull(drawingView.Handler);

		Assert.Equal(1, drawingViewHandler.MapLineWidthCount);

		((DrawingView)drawingView).LineWidth = 1;
		Assert.Equal(2, drawingViewHandler.MapLineWidthCount);
	}

	[Fact]
	public void ClearOnFinishMapperIsCalled()
	{
		var drawingViewHandler = CreateElementHandler<MockDrawingViewHandler>(drawingView);
		Assert.NotNull(drawingView.Handler);

		Assert.Equal(1, drawingViewHandler.MapClearOnFinishCount);

		((DrawingView)drawingView).ClearOnFinish = true;
		Assert.Equal(2, drawingViewHandler.MapClearOnFinishCount);
	}

	[Fact]
	public void LineColorMapperIsCalled()
	{
		var drawingViewHandler = CreateElementHandler<MockDrawingViewHandler>(drawingView);
		Assert.NotNull(drawingView.Handler);

		Assert.Equal(1, drawingViewHandler.MapLineColorCount);

		((DrawingView)drawingView).LineColor = Colors.Blue;
		Assert.Equal(2, drawingViewHandler.MapLineColorCount);
	}

	[Fact]
	public void MultiLineModeMapperIsCalled()
	{
		var drawingViewHandler = CreateElementHandler<MockDrawingViewHandler>(drawingView);
		Assert.NotNull(drawingView.Handler);

		Assert.Equal(1, drawingViewHandler.MapMultiLineModeCount);

		((DrawingView)drawingView).MultiLineMode = true;
		Assert.Equal(2, drawingViewHandler.MapMultiLineModeCount);
	}

	[Fact]
	public void GetImageStreamReturnsNullStream()
	{
		var stream = drawingView.GetImageStream(10, 10);
		Assert.Equal(Stream.Null, stream);
	}
}