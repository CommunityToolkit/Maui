using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingViewHandlerTests : BaseTest
{

	[Fact]
	public void MapLines_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLines(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapLines_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLines(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapShouldClearOnFinish_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapShouldClearOnFinish(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapShouldClearOnFinish_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapShouldClearOnFinish(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapLineColor_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLineColor(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapLineColor_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLineColor(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapLineWidth_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLineWidth(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapLineWidth_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapLineWidth(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapIsMultiLineModeEnabled_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapIsMultiLineModeEnabled(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapIsMultiLineModeEnabled_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapIsMultiLineModeEnabled(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapDrawAction_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapDrawAction(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapDrawAction_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapDrawAction(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void MapDrawingViewBackground_NullHandler_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapDrawingViewBackground(null, new DrawingView()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("handler", exception.ParamName);
	}

	[Fact]
	public void MapDrawingViewBackground_NullView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => DrawingViewHandler.MapDrawingViewBackground(new DrawingViewHandler(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("view", exception.ParamName);
	}

	[Fact]
	public void SetDrawingLineAdapter_NullAdapter_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new DrawingViewHandler().SetDrawingLineAdapter(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("drawingLineAdapter", exception.ParamName);
	}
}