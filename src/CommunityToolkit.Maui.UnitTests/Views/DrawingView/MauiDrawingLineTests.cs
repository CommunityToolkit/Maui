using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.DrawingView;

public class MauiDrawingLineTests : BaseHandlerTest
{
	readonly MauiDrawingLine drawingLine = new();

	[Fact]
	public void OnPointsCollectionChanged()
	{
		drawingLine.Points.Should().BeEmpty();

		drawingLine.Points.Add(new PointF());
		drawingLine.Points.Should().HaveCount(1);

		drawingLine.Points.Add(new PointF());
		drawingLine.Points.Should().HaveCount(2);
	}

	[Fact]
	public void LineWidth()
	{
		drawingLine.LineWidth.Should().Be(5);

		drawingLine.LineWidth = 10;

		drawingLine.LineWidth.Should().Be(10);
	}

	[Fact]
	public void LineColor()
	{
		drawingLine.LineColor.Should().Be(Colors.Black);

		drawingLine.LineColor = Colors.Red;

		drawingLine.LineColor.Should().Be(Colors.Red);
	}

	[Fact]
	public void EnableSmoothedPath()
	{
		drawingLine.ShouldSmoothPathWhenDrawn.Should().BeTrue();

		drawingLine.ShouldSmoothPathWhenDrawn = false;

		drawingLine.ShouldSmoothPathWhenDrawn.Should().BeFalse();
	}

	[Theory]
	[InlineData(10, 10)]
	[InlineData(4, 5)]
	[InlineData(int.MaxValue, int.MaxValue)]
	public void GranularityCheckRange(int value, int expectedValue)
	{
		drawingLine.Granularity.Should().Be(5);

		drawingLine.Granularity = value;

		drawingLine.Granularity.Should().Be(expectedValue);
	}

	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new MauiDrawingLine
		{
			LineColor = DrawingViewDefaults.LineColor,
			LineWidth = DrawingViewDefaults.LineWidth,
			ShouldSmoothPathWhenDrawn = DrawingViewDefaults.ShouldSmoothPathWhenDrawn,
			Granularity = DrawingViewDefaults.MinimumGranularity
		};

		drawingLine.Should().BeEquivalentTo(expectedDefaultValue);
	}
}