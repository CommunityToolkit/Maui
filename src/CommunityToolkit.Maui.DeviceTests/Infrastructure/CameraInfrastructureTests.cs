using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Handlers;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// None of them creates a CameraView platform view: connecting it starts a camera session (and a permission prompt).
[Category(TestCategory.DeviceTestsInfrastructure)]
[Collection(TestCollections.DisableParallelization)]
public class CameraInfrastructureTests : CameraHandlerTestBase
{
	[Fact]
	public void RunnerAppIsThePlatformApplicationWithTheCameraProvider()
	{
		var platformApplication = IPlatformApplication.Current;

		Assert.NotNull(platformApplication);
		Assert.Same(Application.Current, platformApplication.Application);
		Assert.NotNull(platformApplication.Services.GetService<ICameraProvider>());
	}

	// Instantiating the handler runs its ICameraProvider lookup through IPlatformApplication.Current, without creating the
	// platform view.
	[Fact]
	public void CameraViewHandlerResolvesFromTheTestMauiApp()
	{
		SetupBuilder();

		Assert.IsType<CameraViewHandler>(MauiContext.Handlers.GetHandler(typeof(CameraView)));
	}
}
