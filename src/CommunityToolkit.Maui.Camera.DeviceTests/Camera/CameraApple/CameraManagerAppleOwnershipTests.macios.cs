using AVFoundation;
using CommunityToolkit.Maui.Core;
using CoreMotion;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// CameraManager.Dispose releases every native object the manager owns. A real preview start creates most of them (device input, photo
// output, rotation coordinator) and needs a camera and its permission, so these tests seed each field with an object of the real type
// (or a native fake of it) and check that Dispose released it.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleOwnershipTests : CameraHandlerTestBase
{
	[Fact]
	public async Task DisposeReleasesEveryNativeObjectTheManagerOwns()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			var platformView = scope.Manager.CreatePlatformView();
			var session = scope.Access.CaptureSession ?? throw new InvalidOperationException("No capture session.");
			var captureInput = new FakeCaptureDeviceInput();
			var photoOutput = new FakePhotoOutput();
			var rotationCoordinator = new FakeRotationCoordinator(90);
			var motionManager = new CMMotionManager();
			using var recordingStream = new MemoryStream();
			scope.Access.CaptureInput = captureInput;
			scope.Access.PhotoOutput = photoOutput;
			scope.Access.RotationCoordinator = rotationCoordinator;
			scope.Access.MotionManager = motionManager;
			scope.Access.VideoRecordingStream = recordingStream;

			scope.Manager.Dispose();

			Assert.Equal(NativeHandle.Zero, platformView.Handle);
			Assert.Equal(NativeHandle.Zero, session.Handle);
			Assert.True(captureInput.WasDisposed);
			Assert.Equal(NativeHandle.Zero, photoOutput.Handle);
			Assert.True(rotationCoordinator.WasDisposed);
			Assert.Equal(NativeHandle.Zero, motionManager.Handle);
			Assert.False(recordingStream.CanWrite);
			Assert.Null(scope.Access.CaptureSession);
			Assert.Null(scope.Access.CaptureInput);
			Assert.Null(scope.Access.PhotoOutput);
			Assert.Null(scope.Access.RotationCoordinator);
			Assert.Null(scope.Access.MotionManager);
			Assert.Null(scope.Access.VideoRecordingStream);
			Assert.Null(scope.Access.CaptureDevice);
			Assert.Null(scope.Access.OrientationDidChangeObserver);
		});
	}

	[Fact]
	public async Task DisposeWithAnInitializedPreviewForgetsTheCaptureDevice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext);
			scope.Manager.CreatePlatformView();
			using var device = new FakeCaptureDevice();
			scope.Access.CaptureDevice = device;

			scope.Manager.Dispose();

			Assert.Null(scope.Access.CaptureDevice);
			// The device belongs to the camera provider: Dispose must not release it.
			Assert.False(device.WasDisposed);
		});
	}
}