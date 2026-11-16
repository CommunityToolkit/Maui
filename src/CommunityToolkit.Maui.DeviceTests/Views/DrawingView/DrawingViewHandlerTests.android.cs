namespace CommunityToolkit.Maui.DeviceTests;

public partial class DrawingViewHandlerTests
{
	[Fact]
	public Task PlatformViewIsCreatedWithTheHandlersAndroidContext()
	{
		var drawingView = CreateDrawingView();

		return Run(drawingView, (handler, platformView) =>
		{
			Assert.Same(MauiProgramDefaults.DefaultContext, platformView.Context);
		});
	}
}