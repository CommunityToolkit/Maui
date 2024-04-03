using System.Collections.ObjectModel;
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
}