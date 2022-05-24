using CommunityToolkit.Maui.Core.Views;
using FluentAssertions;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views.DrawingView;

public class MauiDrawingViewTests : BaseHandlerTest
{
	readonly MauiDrawingView drawingView = new();

	[Fact]
	public void CheckDefaultValues()
	{
		var expectedDefaultValue = new MauiDrawingView
		{
			ShouldClearOnFinish = false,
			DrawAction = null,
			LineColor = Colors.Black,
			LineWidth = 5f,
			IsMultiLineModeEnabled = false
		};

		drawingView.Should().BeEquivalentTo(expectedDefaultValue);
	}
}