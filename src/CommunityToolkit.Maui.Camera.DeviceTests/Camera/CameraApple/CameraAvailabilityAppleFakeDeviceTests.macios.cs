using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraViewExtensions.UpdateAvailability (macios) and CameraViewHandler.MapIsAvailable on iOS and Mac Catalyst with a scripted default
// video device (AppleDefaultDeviceStub): both answers of AVCaptureDevice.GetDefaultDevice(Video) run on every machine, with or without a
// camera. CameraViewExtensionsAppleTests checks the same code against the real devices of the machine.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraAvailabilityAppleFakeDeviceTests : CameraHandlerTestBase
{
	[Fact]
	public async Task UpdateAvailabilityIsTrueWhenTheSystemReportsADefaultVideoDevice()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice();
			using var view = new CameraView();
			Assert.False(view.IsAvailable);
			using var stub = new AppleDefaultDeviceStub(video: device);

			view.UpdateAvailability();

			Assert.True(view.IsAvailable);
			Assert.Equal([AppleMediaTypes.Video], stub.RequestedMediaTypes);
		});
	}

	[Fact]
	public async Task UpdateAvailabilityIsFalseWhenTheSystemReportsNoDefaultVideoDevice()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			using var view = new CameraView();
			((ICameraView)view).IsAvailable = true;
			using var stub = new AppleDefaultDeviceStub(video: null);

			view.UpdateAvailability();

			Assert.False(view.IsAvailable);
			Assert.Equal([AppleMediaTypes.Video], stub.RequestedMediaTypes);
		});
	}

	[Fact]
	public async Task UpdateAvailabilityIgnoresTheMicrophone()
	{
		await InvokeOnMainThreadAsync(() =>
		{
			using var microphone = new FakeCaptureDevice();
			using var view = new CameraView();
			((ICameraView)view).IsAvailable = true;
			using var stub = new AppleDefaultDeviceStub(video: null, audio: microphone);

			view.UpdateAvailability();

			Assert.False(view.IsAvailable);
			Assert.DoesNotContain(AppleMediaTypes.Audio, stub.RequestedMediaTypes);
		});
	}

	[Fact]
	public async Task HandlerMapsAvailabilityFromTheDefaultVideoDeviceWhenCreated()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice();
			var view = new CameraView();
			Assert.False(view.IsAvailable);

			using (new AppleDefaultDeviceStub(video: device))
			{
				CreateHandler<AppleNoConnectCameraViewHandler>(view);
			}

			Assert.True(view.IsAvailable);
			DisconnectAndRelease(view);
		});
	}

	[Fact]
	public async Task HandlerRecomputesAvailabilityWheneverItIsChanged()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var device = new FakeCaptureDevice();
			var view = new CameraView();
			using var stub = new AppleDefaultDeviceStub(video: device);
			CreateHandler<AppleNoConnectCameraViewHandler>(view);
			Assert.True(view.IsAvailable);

			// IsAvailable is mapped: every change makes the handler ask the system again and overrides the value with its answer.
			((ICameraView)view).IsAvailable = false;
			Assert.True(view.IsAvailable);

			// The camera disappears.
			stub.Video = null;
			((ICameraView)view).IsAvailable = false;
			((ICameraView)view).IsAvailable = true;
			Assert.False(view.IsAvailable);

			Assert.All(stub.RequestedMediaTypes, mediaType => Assert.Equal(AppleMediaTypes.Video, mediaType));
			DisconnectAndRelease(view);
		});
	}

	static void DisconnectAndRelease(CameraView view)
	{
		view.Handler?.DisconnectHandler();
		view.Handler = null;
		view.Dispose();
	}
}