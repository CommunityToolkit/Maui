using AVFoundation;
using CommunityToolkit.Maui.Core;
using Foundation;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// A real AVFoundation session only reaches the successful start of the camera preview with a camera and the camera permission. The session, the
// camera, the device input and the rotation coordinator are fakes and stubs, so every step of the start runs on any machine.
// CameraManagerAppleCaptureTests runs the real start where a camera is authorized.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerApplePreviewTests : CameraHandlerTestBase
{
	[Fact]
	public async Task StartCameraPreviewConfiguresTheSessionAndStartsIt()
	{
		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			var access = preview.Scope.Access;
			Assert.Equal(["begin", "add-input", "add-output", "commit", "start"], preview.Session.Calls);
			Assert.Same(preview.Device, Assert.Single(preview.Inputs.Devices));
			var input = Assert.Single(preview.Session.Inputs);
			Assert.Equal(Assert.Single(preview.Inputs.CreatedInputs), input.Handle.Handle);
			Assert.Same(input, access.CaptureInput);
			Assert.Same(preview.Device, access.CaptureDevice);
			var photoOutput = Assert.IsType<AVCapturePhotoOutput>(Assert.Single(preview.Session.Outputs), exactMatch: false);
			Assert.Same(photoOutput, access.PhotoOutput);
			Assert.True(preview.Session.IsRunning);
			Assert.False(preview.Session.IsConfiguring);
			Assert.True(access.IsInitialized);
			Assert.Equal(1, preview.Scope.LoadedCount);
			Assert.Same(preview.Camera, preview.Scope.View.SelectedCamera);
		});
	}

	[Fact]
	public async Task StartCameraPreviewCreatesARotationCoordinatorForTheCameraAndThePreviewLayer()
	{
		Skip.IfNot(UIKit.UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "AVCaptureDeviceRotationCoordinator exists from iOS 17; StartCameraPreviewBeforeIos17CreatesNoRotationCoordinator covers older versions.");

		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			var created = Assert.Single(preview.Coordinators.Created);
			Assert.Same(preview.Device, created.Device);
			Assert.Equal(AppleNativeHandles.Of(preview.Scope.PlatformView?.Layer), created.PreviewLayer);
			Assert.NotEqual(IntPtr.Zero, created.PreviewLayer);
			Assert.Equal(created.Coordinator, AppleNativeHandles.Of(preview.Scope.Access.RotationCoordinator));
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAPreviewViewCreatesTheRotationCoordinatorWithoutALayer()
	{
		Skip.IfNot(UIKit.UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "AVCaptureDeviceRotationCoordinator exists from iOS 17.");

		await RunAsync(async preview =>
		{
			Assert.Null(preview.Scope.Access.PreviewView);

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			var created = Assert.Single(preview.Coordinators.Created);
			Assert.Same(preview.Device, created.Device);
			Assert.Equal(IntPtr.Zero, created.PreviewLayer);
			Assert.True(preview.Scope.Access.IsInitialized);
		}, createPlatformView: false);
	}

	[Fact]
	public async Task StartCameraPreviewOnIos17ItselfCreatesARotationCoordinator()
	{
		await RunAsync(async preview =>
		{
			using (new AppleSystemVersionOverride(new Version(17, 0)))
			{
				await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			}

			var created = Assert.Single(preview.Coordinators.Created);
			Assert.Same(preview.Device, created.Device);
			Assert.Equal(created.Coordinator, AppleNativeHandles.Of(preview.Scope.Access.RotationCoordinator));
		});
	}

	[Fact]
	public async Task StartCameraPreviewBeforeIos17CreatesNoRotationCoordinator()
	{
		await RunAsync(async preview =>
		{
			using (new AppleSystemVersionOverride(new Version(16, 4)))
			{
				await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			}

			Assert.Empty(preview.Coordinators.Created);
			Assert.Null(preview.Scope.Access.RotationCoordinator);
			Assert.Equal(["begin", "add-input", "add-output", "commit", "start"], preview.Session.Calls);
			Assert.True(preview.Scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StartCameraPreviewSelectsTheFirstProviderCameraWhenNoCameraIsSelected()
	{
		using var secondDevice = new FakeCaptureDevice { Identifier = "fake-preview-second" };
		var second = AppleFakeCameras.Create(secondDevice);

		await RunAsync(async preview =>
		{
			preview.Provider.AvailableCameras = [preview.Camera, second];
			Assert.Null(preview.Scope.View.SelectedCamera);

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Same(preview.Camera, preview.Scope.View.SelectedCamera);
			Assert.Same(preview.Device, Assert.Single(preview.Inputs.Devices));
			Assert.Equal(0, preview.Provider.RefreshCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewKeepsTheSelectedCameraInsteadOfTheFirstProviderCamera()
	{
		using var selectedDevice = new FakeCaptureDevice { Identifier = "fake-preview-selected" };
		var selected = AppleFakeCameras.Create(selectedDevice);

		await RunAsync(async preview =>
		{
			preview.Scope.View.SelectedCamera = selected;

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Same(selected, preview.Scope.View.SelectedCamera);
			Assert.Same(selectedDevice, Assert.Single(preview.Inputs.Devices));
			Assert.Same(selectedDevice, preview.Scope.Access.CaptureDevice);
		});
	}

	[Fact]
	public async Task StartCameraPreviewAppliesTheImageCaptureResolutionInsideTheConfiguration()
	{
		await RunAsync(async preview =>
		{
			var timeline = new List<string>();
			preview.Session.Timeline = timeline;
			preview.Device.Timeline = timeline;
			preview.Scope.View.ImageCaptureResolution = new Size(1280, 720);

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(
				["session:begin", "session:add-input", "session:add-output", "device:lock", "device:format=1280x720", "device:unlock", "session:commit", "session:start"],
				timeline);
		});
	}

	// The default ImageCaptureResolution, Size.Zero, fits no format. Real iPhones start this way since #2941 (#2920, #3037), whatever photo
	// pixel formats the photo output reports.
	[Fact]
	public async Task StartCameraPreviewWithTheDefaultImageCaptureResolutionKeepsTheFormatOfTheSessionPreset()
	{
		await RunAsync(async preview =>
		{
			var timeline = new List<string>();
			preview.Session.Timeline = timeline;
			preview.Device.Timeline = timeline;
			var jpegPixelFormat = NSNumber.FromUInt32(preview.Camera.SupportedFormats[0].FormatDescription.MediaSubType);
			var photoOutput = new FakePhotoOutput { SupportedPixelFormats = [jpegPixelFormat] };
			preview.Scope.Access.PhotoOutput = photoOutput;
			Assert.Equal(Size.Zero, preview.Scope.View.ImageCaptureResolution);

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(["session:begin", "session:add-input", "device:lock", "device:unlock", "session:commit", "session:start"], timeline);
			Assert.Null(preview.Device.CurrentFormat);
			Assert.Empty(photoOutput.RequestedFileTypes);
			Assert.True(preview.Scope.Access.IsInitialized);
		});
	}

	// The raise of setActiveFormat comes from FakeCaptureDevice's strict mode (see
	// CameraManagerAppleFakeDeviceTests.UpdateCaptureResolutionThatTheDeviceRejectsStillUnlocksTheDevice): the camera's formats are not the
	// device's formats, as with a CameraInfo that the app built.
	[Fact]
	public async Task StartCameraPreviewWithAFormatThatTheDeviceRejectsClosesTheConfigurationAndUnlocksTheDevice()
	{
		await RunAsync(async preview =>
		{
			preview.Device.RejectsFormatsOfOtherDevices = true;
			preview.Scope.View.ImageCaptureResolution = new Size(1280, 720);

			// Whether the raise should reach the caller is not the point here: only that the session and the device are released.
			_ = await Record.ExceptionAsync(() => preview.Scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Contains("format-rejected", preview.Device.Calls);
			Assert.False(preview.Session.IsConfiguring);
			Assert.False(preview.Device.IsLocked);
		});
	}

	[Fact]
	public async Task ConnectCameraRefreshesTheProviderSelectsItsFirstCameraAndStartsTheSession()
	{
		await RunAsync(async preview =>
		{
			preview.Provider.AvailableCameras = null;
			preview.Provider.CamerasAfterRefresh = [preview.Camera];

			await preview.Scope.Manager.ConnectCamera(CancellationToken.None);

			Assert.Equal(1, preview.Provider.RefreshCount);
			Assert.Same(preview.Camera, preview.Scope.View.SelectedCamera);
			Assert.Equal(["begin", "add-input", "add-output", "commit", "start"], preview.Session.Calls);
			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.Equal(1, preview.Scope.LoadedCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewTwiceReusesThePhotoOutputAndReplacesTheInput()
	{
		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			var firstInput = preview.Scope.Access.CaptureInput ?? throw new InvalidOperationException("No device input.");
			var photoOutput = preview.Scope.Access.PhotoOutput;

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(["begin", "add-input", "add-output", "commit", "start", "begin", "remove-input", "add-input", "commit", "start"], preview.Session.Calls);
			Assert.Same(firstInput, Assert.Single(preview.Session.RemovedInputs));
			Assert.Equal(NativeHandle.Zero, firstInput.Handle);
			Assert.Equal(2, preview.Inputs.CreatedInputs.Count);
			Assert.Equal(preview.Inputs.CreatedInputs[1], Assert.Single(preview.Session.Inputs).Handle.Handle);
			Assert.Same(photoOutput, Assert.Single(preview.Session.Outputs));
			Assert.Equal(2, preview.Scope.LoadedCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewTwiceReplacesTheRotationCoordinator()
	{
		Skip.IfNot(UIKit.UIDevice.CurrentDevice.CheckSystemVersion(17, 0), "AVCaptureDeviceRotationCoordinator exists from iOS 17.");

		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			var firstCoordinator = preview.Scope.Access.RotationCoordinator ?? throw new InvalidOperationException("No rotation coordinator.");

			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Equal(2, preview.Coordinators.Created.Count);
			Assert.Equal(NativeHandle.Zero, firstCoordinator.Handle);
			Assert.Equal(preview.Coordinators.Created[1].Coordinator, AppleNativeHandles.Of(preview.Scope.Access.RotationCoordinator));
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraWhileInitializedStopsThePreviewAndRestartsItWithTheNewCamera()
	{
		using var otherDevice = new FakeCaptureDevice { Identifier = "fake-preview-other", DevicePosition = AVCaptureDevicePosition.Front };
		var other = AppleFakeCameras.Create(otherDevice, CameraPosition.Front);

		await RunAsync(async preview =>
		{
			preview.Provider.AvailableCameras = [preview.Camera, other];
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			preview.Scope.View.SelectedCamera = other;

			await preview.Scope.Manager.UpdateCurrentCamera(other, CancellationToken.None);

			Assert.Equal(["begin", "add-input", "add-output", "commit", "start", "stop", "begin", "remove-input", "add-input", "commit", "start"], preview.Session.Calls);
			Assert.Equal([preview.Device, otherDevice], preview.Inputs.Devices);
			Assert.Same(otherDevice, preview.Scope.Access.CaptureDevice);
			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.Equal(2, preview.Scope.LoadedCount);
		});
	}

	// A failed camera switch leaves the session stopped but the manager initialized, like on Android. Before, the manager counted as stopped and
	// ignored every later SelectedCamera until StartCameraPreview.
	[Theory]
	[InlineData("noCaptureDevice")]
	[InlineData("inputCannotBeCreated")]
	[InlineData("sessionRejectsTheInput")]
	public async Task UpdateCurrentCameraAfterASwitchThatFailedStartsTheNextSelectedCamera(string failure)
	{
		using var failingDevice = new FakeCaptureDevice { Identifier = "fake-preview-failing", DevicePosition = AVCaptureDevicePosition.Front };
		using var nextDevice = new FakeCaptureDevice { Identifier = "fake-preview-next", DevicePosition = AVCaptureDevicePosition.Front };
		var failingCamera = failure is "noCaptureDevice"
			? AppleFakeCameras.WithoutCaptureDevice("fake-preview-without-device", CameraPosition.Front)
			: AppleFakeCameras.Create(failingDevice, CameraPosition.Front);
		var nextCamera = AppleFakeCameras.Create(nextDevice, CameraPosition.Front);

		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			preview.Inputs.FailureFor = device => failure is "inputCannotBeCreated" && ReferenceEquals(device, failingDevice) ? AppleDeviceInputStub.CreateError("Cannot use the camera") : null;
			preview.Session.CanAddInputResult = failure is not "sessionRejectsTheInput";
			preview.Scope.View.SelectedCamera = failingCamera;

			await Assert.ThrowsAsync<CameraException>(async () => await preview.Scope.Manager.UpdateCurrentCamera(failingCamera, CancellationToken.None));

			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.False(preview.Session.IsRunning);
			Assert.False(preview.Session.IsConfiguring);
			Assert.Empty(preview.Session.Inputs);
			Assert.Equal(1, preview.Scope.LoadedCount);

			preview.Session.CanAddInputResult = true;
			preview.Scope.View.SelectedCamera = nextCamera;
			await preview.Scope.Manager.UpdateCurrentCamera(nextCamera, CancellationToken.None);

			Assert.Same(nextDevice, preview.Inputs.Devices[^1]);
			Assert.Same(nextDevice, preview.Scope.Access.CaptureDevice);
			Assert.Equal(preview.Inputs.CreatedInputs[^1], Assert.Single(preview.Session.Inputs).Handle.Handle);
			Assert.True(preview.Session.IsRunning);
			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.Equal(2, preview.Scope.LoadedCount);
		});
	}

	[Fact]
	public async Task StopCameraPreviewAfterASwitchThatFailedKeepsThePreviewStoppedWhenAnotherCameraIsSelected()
	{
		using var nextDevice = new FakeCaptureDevice { Identifier = "fake-preview-after-stop" };
		var failingCamera = AppleFakeCameras.WithoutCaptureDevice("fake-preview-failing-before-stop");
		var nextCamera = AppleFakeCameras.Create(nextDevice);

		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			preview.Scope.View.SelectedCamera = failingCamera;
			await Assert.ThrowsAsync<CameraException>(async () => await preview.Scope.Manager.UpdateCurrentCamera(failingCamera, CancellationToken.None));
			var callsBeforeTheStop = preview.Session.Calls.Count;

			preview.Scope.Manager.StopCameraPreview();
			preview.Scope.View.SelectedCamera = nextCamera;
			await preview.Scope.Manager.UpdateCurrentCamera(nextCamera, CancellationToken.None);

			Assert.False(preview.Scope.Access.IsInitialized);
			Assert.Equal(callsBeforeTheStop, preview.Session.Calls.Count);
			Assert.DoesNotContain(nextDevice, preview.Inputs.Devices);
			Assert.Equal(1, preview.Scope.LoadedCount);
		});
	}

	// A cancelled switch is not a camera that cannot be started: like a cancelled StartCameraPreview it leaves the manager stopped. The start awaits
	// nothing here that observes the token, so the cancellation is scripted as the session's answer to adding the camera input.
	[Fact]
	public async Task UpdateCurrentCameraThatIsCancelledLeavesThePreviewStopped()
	{
		using var cancelledDevice = new FakeCaptureDevice { Identifier = "fake-preview-cancelled" };
		using var nextDevice = new FakeCaptureDevice { Identifier = "fake-preview-after-cancel" };
		var cancelledCamera = AppleFakeCameras.Create(cancelledDevice);
		var nextCamera = AppleFakeCameras.Create(nextDevice);

		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			preview.Session.Failures["add-input"] = new OperationCanceledException();
			preview.Scope.View.SelectedCamera = cancelledCamera;

			await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await preview.Scope.Manager.UpdateCurrentCamera(cancelledCamera, CancellationToken.None));

			Assert.False(preview.Scope.Access.IsInitialized);
			Assert.False(preview.Session.IsRunning);
			Assert.False(preview.Session.IsConfiguring);

			preview.Session.Failures.Clear();
			preview.Scope.View.SelectedCamera = nextCamera;
			await preview.Scope.Manager.UpdateCurrentCamera(nextCamera, CancellationToken.None);

			Assert.False(preview.Scope.Access.IsInitialized);
			Assert.DoesNotContain(nextDevice, preview.Inputs.Devices);
			Assert.Equal(1, preview.Scope.LoadedCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewThatCannotAddTheInputRaisesErrorOccurredAfterClosingTheConfiguration()
	{
		await RunAsync(async preview =>
		{
			preview.Session.CanAddInputResult = false;

			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(preview.Scope.View, () => preview.Scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("Unable to add capture device input to capture session.", exception.Message);
			Assert.Equal(["begin", "commit"], preview.Session.Calls);
			Assert.False(preview.Session.IsConfiguring);
			Assert.Single(preview.Inputs.CreatedInputs);
			Assert.Null(preview.Scope.Access.CaptureInput);
			Assert.Empty(preview.Session.Inputs);
			Assert.Null(preview.Scope.Access.PhotoOutput);
			Assert.False(preview.Session.IsRunning);
			Assert.Equal(0, preview.Scope.LoadedCount);

			// The camera cannot be started, but the preview counts as started: the next SelectedCamera starts it.
			Assert.True(preview.Scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWhenTheInputCannotBeCreatedRaisesErrorOccurredWithTheNativeError()
	{
		await RunAsync(async preview =>
		{
			preview.Inputs.FailureFor = _ => AppleDeviceInputStub.CreateError("Cannot use the camera");
			CameraException exception;

			// The same failure with ThrowOnInitFailure = false; with the default see
			// CameraManagerAppleSessionTests.StartCameraPreviewWhenTheCameraInputCannotBeCreatedRaisesErrorOccurred.
			using (new AppleInitFailureBehavior(throwOnInitFailure: false))
			{
				exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(preview.Scope.View, () => preview.Scope.Manager.StartCameraPreview(CancellationToken.None));
			}

			Assert.Equal("Error creating capture device input: Cannot use the camera", exception.Message);
			Assert.Equal(["begin", "commit"], preview.Session.Calls);
			Assert.Empty(preview.Inputs.CreatedInputs);
			Assert.Null(preview.Scope.Access.CaptureInput);
			Assert.Equal(0, preview.Scope.LoadedCount);

			// The camera cannot be opened, but the preview counts as started: the next SelectedCamera starts it.
			Assert.True(preview.Scope.Access.IsInitialized);
		});
	}

	[Theory]
	[InlineData("noCaptureDevice")]
	[InlineData("inputCannotBeCreated")]
	[InlineData("sessionRejectsTheInput")]
	public async Task StartCameraPreviewThatFailsBecauseOfTheCameraStartsTheNextSelectedCamera(string failure)
	{
		using var failingDevice = new FakeCaptureDevice { Identifier = "fake-first-start-failing", DevicePosition = AVCaptureDevicePosition.Front };
		using var nextDevice = new FakeCaptureDevice { Identifier = "fake-first-start-next", DevicePosition = AVCaptureDevicePosition.Front };
		var failingCamera = failure is "noCaptureDevice"
			? AppleFakeCameras.WithoutCaptureDevice("fake-first-start-without-device", CameraPosition.Front)
			: AppleFakeCameras.Create(failingDevice, CameraPosition.Front);
		var nextCamera = AppleFakeCameras.Create(nextDevice, CameraPosition.Front);

		await RunAsync(async preview =>
		{
			preview.Inputs.FailureFor = device => failure is "inputCannotBeCreated" && ReferenceEquals(device, failingDevice) ? AppleDeviceInputStub.CreateError("Cannot use the camera") : null;
			preview.Session.CanAddInputResult = failure is not "sessionRejectsTheInput";
			preview.Scope.View.SelectedCamera = failingCamera;

			await CameraErrorRecorder.AssertReportsAsync<CameraException>(preview.Scope.View, () => preview.Scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.False(preview.Session.IsRunning);
			Assert.False(preview.Session.IsConfiguring);
			Assert.Empty(preview.Session.Inputs);
			Assert.Equal(0, preview.Scope.LoadedCount);

			preview.Session.CanAddInputResult = true;
			preview.Scope.View.SelectedCamera = nextCamera;
			await preview.Scope.Manager.UpdateCurrentCamera(nextCamera, CancellationToken.None);

			Assert.Same(nextDevice, preview.Inputs.Devices[^1]);
			Assert.Same(nextDevice, preview.Scope.Access.CaptureDevice);
			Assert.Equal(preview.Inputs.CreatedInputs[^1], Assert.Single(preview.Session.Inputs).Handle.Handle);
			Assert.True(preview.Session.IsRunning);
			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.Equal(1, preview.Scope.LoadedCount);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWithoutTheCameraPermissionLeavesThePreviewStoppedUntilItIsStartedAgain()
	{
		using var nextDevice = new FakeCaptureDevice { Identifier = "fake-after-denied-permission", DevicePosition = AVCaptureDevicePosition.Front };
		var nextCamera = AppleFakeCameras.Create(nextDevice, CameraPosition.Front);

		await RunAsync(async preview =>
		{
			preview.Inputs.FailureFor = _ => CreateNotAuthorizedError();

			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(preview.Scope.View, () => preview.Scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("Camera permission is not granted. Please enable it in the app settings.", exception.Message);
			Assert.False(preview.Scope.Access.IsInitialized);
			Assert.False(preview.Session.IsConfiguring);
			Assert.Empty(preview.Session.Inputs);

			preview.Scope.View.SelectedCamera = nextCamera;
			await preview.Scope.Manager.UpdateCurrentCamera(nextCamera, CancellationToken.None);

			Assert.DoesNotContain(nextDevice, preview.Inputs.Devices);
			Assert.False(preview.Session.IsRunning);
			Assert.False(preview.Scope.Access.IsInitialized);

			preview.Inputs.FailureFor = static _ => null;
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);

			Assert.Same(nextDevice, preview.Scope.Access.CaptureDevice);
			Assert.True(preview.Session.IsRunning);
			Assert.True(preview.Scope.Access.IsInitialized);
			Assert.Equal(1, preview.Scope.LoadedCount);
		});
	}

	// The error with which -[AVCaptureDeviceInput initWithDevice:error:] refuses a camera that the app may not use (the camera permission was
	// denied or is restricted).
	static NSError CreateNotAuthorizedError() =>
		new(AVErrorKeys.ErrorDomain, (nint)(long)AVError.ApplicationIsNotAuthorizedToUseDevice,
			NSDictionary.FromObjectAndKey(new NSString("Cannot use FaceTime HD Camera"), NSError.LocalizedDescriptionKey));

	[Fact]
	public async Task DisposeAfterAStartedPreviewReleasesTheInputTheCoordinatorAndThePhotoOutput()
	{
		await RunAsync(async preview =>
		{
			await preview.Scope.Manager.StartCameraPreview(CancellationToken.None);
			var access = preview.Scope.Access;
			var input = access.CaptureInput ?? throw new InvalidOperationException("No device input.");
			var photoOutput = access.PhotoOutput ?? throw new InvalidOperationException("No photo output.");
			var coordinator = access.RotationCoordinator;

			preview.Scope.Manager.Dispose();

			// The recording cleanup opens and closes a configuration block before the running session is stopped. (AVCaptureSession's own
			// -dealloc then removes the inputs and outputs that the session still reports, which the fake records after "stop".)
			Assert.Equal(["begin", "add-input", "add-output", "commit", "start", "begin", "commit", "stop"], preview.Session.Calls.Take(8));
			Assert.True(preview.Session.WasDisposed);
			Assert.Equal(NativeHandle.Zero, input.Handle);
			Assert.Equal(NativeHandle.Zero, photoOutput.Handle);
			Assert.True(coordinator is null || coordinator.Handle == NativeHandle.Zero);
			Assert.Null(access.CaptureInput);
			Assert.Null(access.PhotoOutput);
			Assert.Null(access.RotationCoordinator);
			Assert.Null(access.CaptureDevice);
			Assert.False(preview.Device.WasDisposed);
		});
	}

	async Task RunAsync(Func<PreviewHarness, Task> test, bool createPlatformView = true)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var device = new FakeCaptureDevice { Identifier = "fake-preview-camera" };
			var formats = new AVCaptureDeviceFormat[] { new FakeCaptureDeviceFormat(640, 480), new FakeCaptureDeviceFormat(1280, 720), new FakeCaptureDeviceFormat(1920, 1080) };
			var camera = AppleFakeCameras.Create(device, CameraPosition.Rear, formats);
			var provider = new AppleFakeCameraProvider { AvailableCameras = [camera] };
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			if (createPlatformView)
			{
				scope.PlatformView = scope.Manager.CreatePlatformView();
			}

			var session = new FakeCaptureSession();
			scope.Access.CaptureSession = session;
			using var inputs = new AppleDeviceInputStub();
			using var coordinators = new AppleRotationCoordinatorStub();

			await test(new PreviewHarness(scope, provider, session, device, camera, inputs, coordinators));
		});
	}

	sealed record PreviewHarness(
		AppleCameraManagerScope Scope,
		AppleFakeCameraProvider Provider,
		FakeCaptureSession Session,
		FakeCaptureDevice Device,
		CameraInfo Camera,
		AppleDeviceInputStub Inputs,
		AppleRotationCoordinatorStub Coordinators);
}