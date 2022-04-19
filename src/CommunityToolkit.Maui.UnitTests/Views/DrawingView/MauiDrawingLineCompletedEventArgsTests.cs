using System.Collections.ObjectModel;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.DrawingView;

public class MauiDrawingLineCompletedEventArgsTests : BaseHandlerTest
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
}