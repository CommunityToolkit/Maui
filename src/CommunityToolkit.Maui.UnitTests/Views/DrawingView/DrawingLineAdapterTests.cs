using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingLineAdapterTests : BaseTest
{
	[Fact]
	public void DrawingViewShouldBeAssignedToIDrawingView()
	{
		var mauiDrawingLine = new MauiDrawingLine
		{
			LineColor = Colors.Blue,
			LineWidth = 10f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 15,
			Points = [new(10, 10)]
		};

		var expectedDrawingLine = new DrawingLine
		{
			LineColor = Colors.Blue,
			LineWidth = 10f,
			ShouldSmoothPathWhenDrawn = false,
			Granularity = 15,
			Points = [new(10, 10)]
		};

		var drawingLineAdapter = new DrawingLineAdapter();
		var result = drawingLineAdapter.ConvertMauiDrawingLine(mauiDrawingLine);

		result.Should().BeEquivalentTo(expectedDrawingLine);
	}

	[Fact]
	public void ConvertMauiDrawingLine_NullMauiDrawingLine_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new DrawingLineAdapter().ConvertMauiDrawingLine(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingLine", exception.ParamName);
	}
}