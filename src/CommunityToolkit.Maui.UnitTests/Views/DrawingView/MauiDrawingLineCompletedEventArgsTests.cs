using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class MauiDrawingLineCompletedEventArgsTests : BaseViewTest
{
	[Fact]
	public void MauiDrawingLineShouldBeEqualInMauiDrawingLineCompletedEventArgs()
	{
		var drawingLine = new MauiDrawingLine()
		{
			ShouldSmoothPathWhenDrawn = true,
			Granularity = 10,
			LineColor = Colors.Blue,
			LineWidth = 10,
			Points = new ObservableCollection<PointF>(new[] { new PointF(10, 10) })
		};
		var drawingLineCompletedEventArgs = new MauiDrawingLineCompletedEventArgs(drawingLine);
		drawingLineCompletedEventArgs.Line.Should().BeEquivalentTo(drawingLine);
	}

	[Fact]
	public void MauiDrawingLineCompletedEventArgs_NullLine_ThrowsArgumentNullException()
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		var exception = Assert.Throws<ArgumentNullException>(() => new MauiDrawingLineCompletedEventArgs(null));
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

		Assert.Equal("line", exception.ParamName);
	}
}