using AVFoundation;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// Tests that need a person: they trigger the system camera prompt. Headless runs never execute them (RequiresUserInteraction is
// in every skip list); start the app without arguments to run them in the visual runner on a Mac. The answer is remembered by the
// system, so allow the camera in the first test once to enable CameraManagerAppleCaptureTests (their permission-gated tests)
// until the app is rebuilt, or deny it in the second one to run the denied-permission path.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraAppleUserInteractionTests : CameraHandlerTestBase
{
	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task AllowingTheCameraPromptLetsTheCameraViewPreviewAndCaptureAPhoto()
	{
		Skip.IfNot(AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var isGranted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
		Skip.IfNot(isGranted, "The camera permission was denied in the prompt; allow it to run this test, or run the denied-permission test instead.");
		SetupBuilder();
		var view = new CameraView { WidthRequest = 320, HeightRequest = 240 };

		await AttachAndRun<CommunityToolkit.Maui.Core.Handlers.CameraViewHandler>(view, async handler =>
		{
			await view.OnLoadedAsync();
			var access = new CameraManagerAccessor(handler.CameraManager);
			await AssertHelpers.AssertEventually(() => access.CaptureSession?.Running is true, timeout: 20000, message: "The camera session did not start after the permission was granted.");

			var stream = await view.CaptureImage(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(20));

			Assert.True(stream.Length > 1000);
			Assert.Equal(0xFF, stream.ReadByte());
			Assert.Equal(0xD8, stream.ReadByte());
		});

		await InvokeOnMainThreadAsync(() =>
		{
			view.Handler?.DisconnectHandler();
			view.Handler = null;
		});
	}

	[Fact]
	[Category(TestCategory.RequiresUserInteraction)]
	public async Task DenyingTheCameraPromptMakesStartVideoRecordingThrowCameraException()
	{
		Skip.IfNot(AppleCameraEnvironment.HasVideoCaptureDevice, $"Needs a camera ({AppleCameraEnvironment.DescribeCameraAccess()}).");
		var isGranted = await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVAuthorizationMediaType.Video);
		Skip.If(isGranted, "The camera permission is granted; tap Don't Allow in the prompt (or reset it with 'tccutil reset Camera <bundle id>') to run this test.");
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();

			var exception = await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None));

			Assert.Equal("Camera permission is not granted. Please enable it in the app settings.", exception.Message);
		});
	}
}