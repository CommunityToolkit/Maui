using AVFoundation;
using CommunityToolkit.Maui.Core;
using ObjCRuntime;

namespace CommunityToolkit.Maui.DeviceTests;

// A real session cannot be configured without a camera input, and on the iOS simulator its StartRunning and StopRunning each take about 9 s, so
// FakeCaptureSession records the calls the manager makes instead.
[Category(TestCategory.CameraApple)]
[Collection(TestCollections.DisableParallelization)]
public class CameraManagerAppleSessionTests : CameraHandlerTestBase
{
	[Fact]
	public async Task StopCameraPreviewStopsARunningSession()
	{
		await RunAsync((scope, session) =>
		{
			session.IsRunning = true;
			scope.Access.IsInitialized = true;

			scope.Manager.StopCameraPreview();

			Assert.Equal(["stop"], session.Calls);
			Assert.False(session.IsRunning);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StopCameraPreviewDoesNotStopASessionThatIsNotRunning()
	{
		await RunAsync((scope, session) =>
		{
			session.IsRunning = false;
			scope.Access.IsInitialized = true;

			scope.Manager.StopCameraPreview();

			Assert.Empty(session.Calls);
			Assert.False(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task DisconnectDoesNotStopOrReleaseTheSession()
	{
		await RunAsync((scope, session) =>
		{
			session.IsRunning = true;
			scope.Access.IsInitialized = true;

			scope.Manager.Disconnect();

			Assert.Empty(session.Calls);
			Assert.True(session.IsRunning);
			Assert.False(session.WasDisposed);
			Assert.True(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task UpdateCurrentCameraStopsTheRunningPreviewBeforeStartingTheNewCamera()
	{
		var camera = new CameraInfo("Camera without device", "fake-switch", CameraPosition.Rear, false, 1f, 4f, [new Size(640, 480)], null!, []);
		await RunAsync(async (scope, session) =>
		{
			session.IsRunning = true;
			scope.Access.IsInitialized = true;
			scope.View.SelectedCamera = camera;

			var exception = await Assert.ThrowsAsync<CameraException>(async () => await scope.Manager.UpdateCurrentCamera(camera, CancellationToken.None));

			// The camera has no device, so the start fails after beginning its configuration and commits it (see
			// StartCameraPreviewThatFailsCommitsTheConfigurationBlock). The manager stays initialized so that the next camera change starts the
			// preview.
			Assert.Equal("No Camera found", exception.Message);
			Assert.Equal(["stop", "begin", "commit"], session.Calls);
			Assert.False(session.IsRunning);
			Assert.True(scope.Access.IsInitialized);
		});
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DisposeClosesTheRecordingConfigurationBlockThenStopsAndReleasesTheSession(bool isRunning)
	{
		await RunAsync((scope, session) =>
		{
			session.IsRunning = isRunning;

			scope.Manager.Dispose();

			// stopRunning may not be called between beginConfiguration and commitConfiguration.
			Assert.Equal(["begin", "commit", "stop"], session.Calls);
			Assert.True(session.WasDisposed);
			Assert.Null(scope.Access.CaptureSession);
		});
	}

	[Fact]
	public async Task DisposeRemovesTheRecordingInputAndOutputFromTheSessionInOneConfigurationBlock()
	{
		await RunAsync((scope, session) =>
		{
			var audioInput = new FakeCaptureDeviceInput();
			var movieOutput = new AVCaptureMovieFileOutput();
			scope.Access.AudioInput = audioInput;
			scope.Access.VideoOutput = movieOutput;

			scope.Manager.Dispose();

			Assert.Equal(["begin", "remove-input", "remove-output", "commit", "stop"], session.Calls);
			Assert.Equal([audioInput], session.RemovedInputs);
			Assert.Equal([movieOutput], session.RemovedOutputs);
			Assert.True(audioInput.WasDisposed);
			Assert.Equal(NativeHandle.Zero, movieOutput.Handle);
			Assert.Null(scope.Access.AudioInput);
			Assert.Null(scope.Access.VideoOutput);
		});
	}

	[Fact]
	public async Task StartCameraPreviewRemovesAndReleasesTheInputsOfThePreviousCamera()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
		await RunAsync(async (scope, session) =>
		{
			var first = new FakeCaptureDeviceInput();
			var second = new FakeCaptureDeviceInput();
			session.SeedInput(first);
			session.SeedInput(second);

			await CameraErrorRecorder.AssertReportsAsync<CameraException>(scope.View, () => scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal(["begin", "remove-input", "remove-input"], session.Calls.Take(3));
			Assert.DoesNotContain("add-input", session.Calls);
			Assert.Equal([first, second], session.RemovedInputs);
			Assert.True(first.WasDisposed);
			Assert.True(second.WasDisposed);
			Assert.Empty(session.Inputs);
		}, provider);
	}

	[Fact]
	public async Task StartCameraPreviewWithoutAnyCameraBeginsTheConfigurationBeforeFailing()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
		await RunAsync(async (scope, session) =>
		{
			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(scope.View, () => scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("No camera available on device", exception.Message);
			Assert.Equal("begin", session.Calls[0]);
			Assert.DoesNotContain("add-input", session.Calls);
			Assert.DoesNotContain("start", session.Calls);
			Assert.False(scope.Access.IsInitialized);
			Assert.Equal(0, scope.LoadedCount);
		}, provider);
	}

	// A start that fails after BeginConfiguration (no camera, no capture device) must still commit the configuration: AVCaptureSession raises
	// NSGenericException for stopRunning (Dispose) between beginConfiguration and commitConfiguration.
	[Fact]
	public async Task StartCameraPreviewThatFailsCommitsTheConfigurationBlock()
	{
		var provider = new AppleFakeCameraProvider { AvailableCameras = [] };
		await RunAsync(async (scope, session) =>
		{
			await CameraErrorRecorder.AssertReportsAsync<CameraException>(scope.View, () => scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.False(session.IsConfiguring);
		}, provider);
	}

	// With the default ObjCRuntime.Class.ThrowOnInitFailure the binding's AVCaptureDeviceInput constructor throws System.Exception ('Could not
	// initialize an instance of the type AVFoundation.AVCaptureDeviceInput: the native initWithDevice:error: method returned nil') and loses the
	// NSError, so a camera in use must still surface as CameraException with the native error (reported through ErrorOccurred: StartCameraPreview
	// does not throw). A denied permission surfaces as the permission (see
	// CameraManagerApplePreviewTests.StartCameraPreviewWithoutTheCameraPermissionLeavesThePreviewStoppedUntilItIsStartedAgain).
	[Fact]
	public async Task StartCameraPreviewWhenTheCameraInputCannotBeCreatedRaisesErrorOccurred()
	{
		using var device = new FakeCaptureDevice();
		var camera = AppleFakeCameras.Create(device);
		var provider = new AppleFakeCameraProvider { AvailableCameras = [camera] };
		await RunAsync(async (scope, session) =>
		{
			using var inputs = new AppleDeviceInputStub { FailureFor = _ => AppleDeviceInputStub.CreateError("Cannot use the camera") };

			var exception = await CameraErrorRecorder.AssertReportsAsync<CameraException>(scope.View, () => scope.Manager.StartCameraPreview(CancellationToken.None));

			Assert.Equal("Error creating capture device input: Cannot use the camera", exception.Message);
			Assert.False(session.IsConfiguring);
			Assert.Null(scope.Access.CaptureInput);
		}, provider);
	}

	[Fact]
	public async Task FakeSessionRejectsADisposedOutputLikeTheRealSession()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			using var realSession = new AVCaptureSession();
			using var fakeSession = new FakeCaptureSession();
			var disposedOutputForReal = new AVCaptureMovieFileOutput();
			disposedOutputForReal.Dispose();
			var disposedOutputForFake = new AVCaptureMovieFileOutput();
			disposedOutputForFake.Dispose();

			var realFailure = Record.Exception(() => realSession.RemoveOutput(disposedOutputForReal));
			var fakeFailure = Record.Exception(() => fakeSession.RemoveOutput(disposedOutputForFake));

			// The fake must fail the way AVFoundation's binding does, or the tests that rely on it would prove nothing.
			Assert.NotNull(realFailure);
			Assert.IsType<ObjectDisposedException>(realFailure);
			Assert.IsType(realFailure.GetType(), fakeFailure);
		});
	}

	async Task RunAsync(Action<AppleCameraManagerScope, FakeCaptureSession> test, ICameraProvider? provider = null) =>
		await RunAsync((scope, session) =>
		{
			test(scope, session);
			return Task.CompletedTask;
		}, provider);

	async Task RunAsync(Func<AppleCameraManagerScope, FakeCaptureSession, Task> test, ICameraProvider? provider = null)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new AppleCameraManagerScope(MauiContext, provider);
			var session = new FakeCaptureSession();
			scope.Access.CaptureSession = session;

			await test(scope, session);
		});
	}
}