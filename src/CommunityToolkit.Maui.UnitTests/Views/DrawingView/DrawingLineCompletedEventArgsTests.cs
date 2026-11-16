using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingLineCompletedEventArgsTests : BaseViewTest
{
	[Fact]
	public void DrawingLineShouldBeEqualInDrawingLineCompletedEventArgs()
	{
		var drawingLine = new DrawingLine()
		{
			ShouldSmoothPathWhenDrawn = true,
			Granularity = 10,
			LineColor = Colors.Blue,
			LineWidth = 10,
			Points = new ObservableCollection<PointF>(new[] { new PointF(10, 10) })
		};
		var drawingLineCompletedEventArgs = new DrawingLineCompletedEventArgs(drawingLine);
		drawingLineCompletedEventArgs.LastDrawingLine.Should().BeEquivalentTo(drawingLine);
	}

	[Fact]
	public void DrawingLineCompletedEventArgs_NullLastDrawingLine_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new DrawingLineCompletedEventArgs(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("lastDrawingLine", exception.ParamName);
	}
}