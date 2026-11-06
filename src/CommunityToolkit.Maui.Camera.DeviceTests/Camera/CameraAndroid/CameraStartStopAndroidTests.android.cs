using AndroidX.Camera.Core;
using CommunityToolkit.Maui.Core;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

// Every start below waits for CameraX, whose answer the UI thread receives in a later message, so a StopCameraPreview in the same message as the
// start lands while the start waits.
[Category(TestCategory.CameraStartStopAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraStartStopAndroidTests : CameraAndroidTestBase
{
	[Fact]
	public async Task StopCameraPreviewWhileStartCameraPreviewWaitsForCameraXCancelsTheStart()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await StopThePreviewAsync(session);
			var loadedBefore = session.LoadedCount;

			var start = session.Manager.StartCameraPreview(CancellationToken.None);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(CameraTimeout));
			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the start.");
			await WaitForTheListenersOfCameraXAsync();
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The start that StopCameraPreview cancelled bound the preview.");
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Empty(errors.Errors);
			await AssertEveryCameraIsClosedAsync();
		});
	}

	// The page closes while the start waits for CameraX: the handler disconnects and releases the manager, so the start fails with the
	// ObjectDisposedException of the released manager. The view is gone, so the app has nothing to show for it.
	[Fact]
	public async Task DisposeWhileStartCameraPreviewWaitsForCameraXEndsTheStartWithoutRaisingErrorOccurred()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await StopThePreviewAsync(session);

			var start = session.Manager.StartCameraPreview(CancellationToken.None);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for CameraX.");
			session.Manager.Dispose();

			await start.WaitAsync(CameraTimeout);
			await WaitForTheListenersOfCameraXAsync();

			Assert.Empty(errors.Errors);
			Assert.Empty(UnhandledAsyncVoidExceptions);
		});
	}

	// The token of the start is linked with the token that StopCameraPreview cancels: the exception still has the type of the platform's
	// cancellation and names the caller's token.
	[Fact]
	public async Task StartCameraPreviewThatItsTokenCancelsWhileItWaitsForCameraXThrowsWithThatToken()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			await StopThePreviewAsync(session);
			using var cancellation = new CancellationTokenSource();

			var start = session.Manager.StartCameraPreview(cancellation.Token);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for CameraX.");
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(CameraTimeout));
			Assert.IsType<TaskCanceledException>(exception);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			await WaitForTheListenersOfCameraXAsync();
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The cancelled start bound the preview.");
		});
	}

	// A start whose camera cannot be bound leaves the preview started for the next selected camera (decision P): the stop must clear that too.
	[Fact]
	public async Task StopCameraPreviewWhileTheStartOfACameraThatCanNotBeBoundWaitsForCameraXKeepsThePreviewStoppedWhenAnotherCameraIsSelected()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			var camera = session.View.SelectedCamera ?? throw new InvalidOperationException("The connected session has no selected camera.");
			await StopThePreviewAsync(session);
			var loadedBefore = session.LoadedCount;
			session.View.SelectedCamera = CreateCameraThatCanNotBeBound();

			var start = session.Manager.StartCameraPreview(CancellationToken.None);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(CameraTimeout));

			await SelectCameraAsync(session, camera);
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "Selecting a camera after the cancelled start started the stopped preview.");
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Empty(errors.Errors);
			await AssertEveryCameraIsClosedAsync();
		});
	}

	[Fact]
	public async Task StopCameraPreviewCommandWhileTheStartCameraPreviewCommandWaitsForCameraXKeepsThePreviewStoppedWithoutAnError()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");
			var preview = host.Native<Preview>("cameraPreview") ?? throw new InvalidOperationException("The manager has no Preview.");

			host.View.StartCameraPreviewCommand.Execute(CancellationToken.None);
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(preview), "The start that the StopCameraPreviewCommand cancelled bound the preview.");
			Assert.Empty(errors.Errors);
			Assert.Empty(UnhandledAsyncVoidExceptions);
			await AssertEveryCameraIsClosedAsync();
		});
	}

	// The camera discovery of a new CameraProvider waits for CameraX as well, before the connection waits for it to prepare the use cases.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StopCameraPreviewWhileTheCameraConnectsKeepsThePreviewStopped(bool duringTheCameraDiscovery)
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var provider = duringTheCameraDiscovery ? null : new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

			var connect = session.Manager.ConnectCamera(CancellationToken.None);
			Assert.False(connect.IsCompleted, "ConnectCamera completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			await connect.WaitAsync(CameraTimeout);
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The connection bound the preview that StopCameraPreview stopped.");
			Assert.Equal(0, session.LoadedCount);
			Assert.Empty(errors.Errors);
			await AssertEveryCameraIsClosedAsync();

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView ?? throw new InvalidOperationException("The session has no PreviewView."));
			Assert.Equal(1, session.LoadedCount);
			Assert.Empty(errors.Errors);
		}, provider);
	}

	// Until the connection has prepared the use cases, a start of the preview could not bind them, and the connection does not start the preview
	// that the stop stopped.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StartCameraPreviewAfterAStopCameraPreviewWhileTheCameraConnectsStartsThePreview(bool duringTheCameraDiscovery)
	{
		SetupBuilder();
		EnsureCameraPermissions();
		var provider = duringTheCameraDiscovery ? null : new StubCameraProvider { AvailableCameras = await GetRealCamerasAsync() };

		await RunWithSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			await session.Manager.UpdateCaptureResolution(Size.Zero, CancellationToken.None);

			var connect = session.Manager.ConnectCamera(CancellationToken.None);
			Assert.False(connect.IsCompleted, "ConnectCamera completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			var start = session.Manager.StartCameraPreview(CancellationToken.None);
			await connect.WaitAsync(CameraTimeout);
			await start.WaitAsync(CameraTimeout);

			var preview = session.Native<Preview>("cameraPreview");
			Assert.True(preview is not null && await IsNativelyBoundAsync(preview), "The StartCameraPreview that followed the StopCameraPreview did not start the preview.");
			await WaitUntilCameraIsStreamingAsync(session.View, session.PreviewView ?? throw new InvalidOperationException("The session has no PreviewView."));
			Assert.Equal(1, session.LoadedCount);
			Assert.Empty(errors.Errors);
		}, provider);
	}

	[Fact]
	public async Task StopCameraPreviewWhileStartVideoRecordingWaitsForCameraXCancelsTheRecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			var videoCapture = session.VideoCapture;

			var recording = session.Manager.StartVideoRecording(new MemoryStream(), CancellationToken.None);
			Assert.False(recording.IsCompleted, "StartVideoRecording completed without waiting for CameraX.");
			session.Manager.StopCameraPreview();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(CameraTimeout));
			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the recording.");
			await WaitForTheListenersOfCameraXAsync();
			Assert.False(await IsNativelyBoundAsync(videoCapture), "The recording that StopCameraPreview cancelled bound the video session.");
			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The recording that StopCameraPreview cancelled started the preview.");
			Assert.Null(session.Native<AndroidX.Camera.Video.Recording>("videoRecording"));
			Assert.Empty(ListRecordingFiles().Except(before));
			Assert.Empty(errors.Errors);
			await WaitForAllCamerasClosedAsync();

			await session.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(session.ImageCapture), "The preview did not start in the photo session after the cancelled recording.");
			Assert.False(await IsNativelyBoundAsync(session.VideoCapture), "The preview started in the video session of the cancelled recording.");
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopCameraPreviewCommandWhileTheStartVideoRecordingCommandWaitsForCameraXRaisesNoEvent()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);
			var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
			EventHandler<MediaCaptureFailedEventArgs> onFailed = (_, e) => failures.Enqueue(e.FailureReason);
			host.View.MediaCaptureFailed += onFailed;
			var videoCapture = host.Native<AndroidX.Camera.Video.VideoCapture>("videoCapture") ?? throw new InvalidOperationException("The manager has no VideoCapture.");

			host.View.StartVideoRecordingCommand.Execute(new MemoryStream());
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForTheListenersOfCameraXAsync();

			Assert.Empty(failures);
			Assert.Empty(errors.Errors);
			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.False(await IsNativelyBoundAsync(videoCapture), "The recording that the StopCameraPreviewCommand cancelled bound the video session.");
			Assert.Empty(ListRecordingFiles().Except(before));
			await WaitForAllCamerasClosedAsync();
			GC.KeepAlive(onFailed);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopCameraPreviewWhileANewCaptureResolutionRestartsThePreviewKeepsThePreviewStopped()
	{
		SetupBuilder();

		await RunWithStreamingSessionAsync(async session =>
		{
			using var errors = new CameraErrorRecorder(session.View);
			var loadedBefore = session.LoadedCount;

			var restart = session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask();
			Assert.False(restart.IsCompleted, "The new capture resolution restarted the preview without waiting for CameraX.");
			session.Manager.StopCameraPreview();
			await restart.WaitAsync(CameraTimeout);
			await WaitForTheListenersOfCameraXAsync();

			Assert.False(await IsNativelyBoundAsync(session.CameraPreview), "The restart for the new capture resolution started the stopped preview.");
			Assert.Equal(loadedBefore, session.LoadedCount);
			Assert.Empty(errors.Errors);
			await WaitForAllCamerasClosedAsync();
		});
	}

	// The restart for a new capture resolution holds CameraX when the user selects another camera: the start of the camera switch waits for the
	// restart, which binds the camera that was selected before. The switch must still open its own camera.
	[Fact]
	public async Task ACameraSwitchWhoseStartWaitedForTheRestartOfTheOldCameraStillOpensTheNewCamera()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		Skip.If(cameras.Count < 2, "The device has only one camera.");

		await RunWithStreamingSessionAsync(async session =>
		{
			var cameraA = session.View.SelectedCamera ?? throw new InvalidOperationException("The connected session has no selected camera.");
			var cameraB = cameras.First(camera => camera.DeviceId != cameraA.DeviceId);

			var restart = session.Manager.UpdateCaptureResolution(SmallResolution, CancellationToken.None).AsTask();
			session.View.SelectedCamera = cameraB;
			var cameraSwitch = session.Manager.UpdateCurrentCamera(cameraB, CancellationToken.None).AsTask();

			await Task.WhenAll(restart, cameraSwitch).WaitAsync(CameraTimeout);
			await WaitForTheListenersOfCameraXAsync();

			await WaitForNativeCameraStateAsync(cameraB.DeviceId, "OPEN");
			await WaitForNativeCameraStateAsync(cameraA.DeviceId, "CLOSED");
		});
	}

	static async Task StopThePreviewAsync(ManagerSession session)
	{
		var cameraId = session.View.SelectedCamera?.DeviceId ?? throw new InvalidOperationException("The connected session has no selected camera.");
		session.Manager.StopCameraPreview();
		await WaitForNativeCameraStateAsync(cameraId, "CLOSED");
	}

	// Stands in for CameraViewHandler.MapSelectedCamera, which a manager session has no handler to run.
	static Task SelectCameraAsync(ManagerSession session, CameraInfo camera)
	{
		session.View.SelectedCamera = camera;
		return session.Manager.UpdateCurrentCamera(camera, CancellationToken.None).AsTask().WaitAsync(CameraTimeout);
	}
}
