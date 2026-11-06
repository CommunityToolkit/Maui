using CommunityToolkit.Maui.Core.Extensions;
using FluentAssertions;
using Xunit;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Views;
using CommunityToolkit.Maui.UnitTests.Mocks;

namespace CommunityToolkit.Maui.UnitTests.Views;

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

	[Fact]
	public void CreateSmoothedPathWithGranularity_NullCurrentPoints_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.CreateSmoothedPathWithGranularity(null, 5));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("currentPoints", exception.ParamName);
	}

	[Fact]
	public void SetIsMultiLineModeEnabled_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetIsMultiLineModeEnabled(null, true));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetDrawAction_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetDrawAction(null, null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetShouldClearOnFinish_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetShouldClearOnFinish(null, true));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetLineColor_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetLineColor(null, Colors.Black));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetLineWidth_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetLineWidth(null, 1));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetPaint_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetPaint(null, Colors.Black.AsPaint()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetLines_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetLines((MauiDrawingView)null, new DrawingView()));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetLines_NullDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new MauiDrawingView().SetLines((IDrawingView)null));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("drawingView", exception.ParamName);
	}

	[Fact]
	public void SetLines_WithAdapter_NullDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => MauiDrawingViewExtensions.SetLines((IDrawingView)null, new MauiDrawingView(), new DrawingLineAdapter()));
#pragma warning restore CS8600, CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("drawingView", exception.ParamName);
	}

	[Fact]
	public void SetLines_WithAdapter_NullMauiDrawingView_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new DrawingView().SetLines(null, new DrawingLineAdapter()));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("mauiDrawingView", exception.ParamName);
	}

	[Fact]
	public void SetLines_WithAdapter_NullAdapter_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new DrawingView().SetLines(new MauiDrawingView(), null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("adapter", exception.ParamName);
	}
}