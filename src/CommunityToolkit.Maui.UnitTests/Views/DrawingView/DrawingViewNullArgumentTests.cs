using CommunityToolkit.Maui.Views;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Views;

public class DrawingViewNullArgumentTests : BaseTest
{
	[Fact]
	public void GetImageStream_NullOptions_ShouldThrowArgumentNullExceptionBeforeItReturnsTheValueTask()
	{
#pragma warning disable CS8600, CS8625 // Converting null literal or possible null value to non-nullable type.
		NullArgumentAssert.Throws("options", () => { _ = DrawingView.GetImageStream((Core.Views.ImageLineOptions)null, TestContext.Current.CancellationToken); });
#pragma warning restore CS8600, CS8625
	}
}