namespace CommunityToolkit.Maui.DeviceTests;

public partial class DrawingViewHandlerTests
{
	// Catches: the platform view is created with another context than the handler's (the activity)
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