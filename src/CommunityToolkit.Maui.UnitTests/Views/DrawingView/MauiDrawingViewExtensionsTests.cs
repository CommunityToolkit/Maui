using CommunityToolkit.Maui.Core.Extensions;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.DrawingView;

public class MauiDrawingViewExtensionsTests : BaseTest
{
	[Fact]
	public void SmoothedPathWithGranularity_NotEnoughPoints_ReturnOriginalCollection()
	{
		var points = new List<PointF>
		{
			new(10, 10),
			new(20, 20)
		};
		var newPoints = points.CreateSmoothedPathWithGranularity(points.Count - 1);
		newPoints.Should().BeEquivalentTo(points);
	}

	[Fact]
	public void SmoothedPathWithGranularity_EnoughPoints_ReturnSmoothedCollection()
	{
		var expectedPoints = new List<PointF>
		{
			new(0, 0),
			new(0.625f, 4.375f),
			new(2, 10),
			new(4.375f, 15.125f),
			new(8, 20),
			new(13.75f, 24.5f),
			new(20, 28),
			new(25.75f, 29.5f),
			new(30, 30),
			new(30, 30)
		};
		var points = new List<PointF>
		{
			new(0, 0),
			new(2, 10),
			new(8, 20),
			new(20, 28),
			new(30, 30)
		};

		var newPoints = points.CreateSmoothedPathWithGranularity(2);
		// each second point should be equivalent
		newPoints.Should().BeEquivalentTo(expectedPoints);
	}
}