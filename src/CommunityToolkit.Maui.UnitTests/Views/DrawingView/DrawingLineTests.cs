using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingLineTests : BaseHandlerTest
{
	readonly IDrawingLine drawingLine = new DrawingLine();

	public DrawingLineTests()
	{
		Assert.IsAssignableFrom<IDrawingLine>(drawingLine);
	}

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
		drawingLine.LineWidth.Should().Be(DrawingViewDefaults.LineWidth);

		drawingLine.LineWidth = 10;

		drawingLine.LineWidth.Should().Be(10);
	}

	[Fact]
	public void LineColor()
	{
		drawingLine.LineColor.Should().Be(DrawingViewDefaults.LineColor);

		drawingLine.LineColor = Colors.Red;

		drawingLine.LineColor.Should().Be(Colors.Red);
	}

	[Fact]
	public void DisableSmoothedPath()
	{
		drawingLine.ShouldSmoothPathWhenDrawn.Should().Be(DrawingViewDefaults.ShouldSmoothPathWhenDrawn);

		drawingLine.ShouldSmoothPathWhenDrawn = false;

		drawingLine.ShouldSmoothPathWhenDrawn.Should().BeFalse();
	}

	[Theory]
	[InlineData(10, 10)]
	[InlineData(4, 5)]
	[InlineData(int.MaxValue, int.MaxValue)]
	public void GranularityCheckRange(int value, int expectedValue)
	{
		drawingLine.Granularity.Should().Be(DrawingViewDefaults.MinimumGranularity);

		drawingLine.Granularity = value;

		drawingLine.Granularity.Should().Be(expectedValue);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenExpired()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await Task.Delay(100, CancellationToken.None);

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await drawingLine.GetImageStream(10, 10, Colors.Blue.AsPaint(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStream_CancellationTokenCanceled()
	{
		var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1));

		// Ensure CancellationToken Expired
		await cts.CancelAsync();

		await Assert.ThrowsAsync<OperationCanceledException>(async () => await drawingLine.GetImageStream(10, 10, Colors.Blue.AsPaint(), cts.Token));
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamReturnsNullStream()
	{
		var stream = await drawingLine.GetImageStream(10, 10, Colors.Blue.AsPaint(), CancellationToken.None);
		Assert.Equal(Stream.Null, stream);
	}

	[Fact(Timeout = (int)TestDuration.Short)]
	public async Task GetImageStreamStaticReturnsNullStream()
	{
		var stream = await DrawingLine.GetImageStream(Array.Empty<PointF>(), new Size(10, 10), 5, Colors.Yellow, Colors.Blue.AsPaint(), CancellationToken.None);
		Assert.Equal(Stream.Null, stream);
	}

	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new DrawingLine
		{
			LineColor = DrawingViewDefaults.LineColor,
			LineWidth = DrawingViewDefaults.LineWidth,
			ShouldSmoothPathWhenDrawn = DrawingViewDefaults.ShouldSmoothPathWhenDrawn,
			Granularity = DrawingViewDefaults.MinimumGranularity,
			Points = []
		};

		drawingLine.Should().BeEquivalentTo(expectedDefaultValue);
	}
}