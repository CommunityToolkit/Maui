using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.DeviceTests;

// The test machines have no camera, so WindowsScriptedMediaFoundation answers the initializations, capture resolutions and recording
// preparations of the manager when the test says so: a StopCameraPreview, or another start, lands while a start waits for Media Foundation.
[Category(TestCategory.CameraWindows)]
[Collection(TestCollections.DisableParallelization)]
public class CameraStartStopWindowsTests : CameraHandlerTestBase
{
	static readonly TimeSpan timeout = TimeSpan.FromSeconds(10);

	// Media Foundation can observe the cancellation of the initialization, fail the initialization of the MediaCapture that the stop released, or
	// complete it all the same.
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("opensTheCameraAllTheSame")]
	public async Task StopCameraPreviewWhileMediaFoundationInitializesTheCameraCancelsTheStart(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			Assert.False(start.IsCompleted, "StartCameraPreview completed without waiting for Media Foundation.");

			scope.Manager.StopCameraPreview();
			Answer(initialization, answer);

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(timeout));
			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the start.");
			Assert.Empty(errors.Errors);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The MediaCapture of the cancelled start was not released.");
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	// A start whose camera cannot be opened leaves the preview started for the next selected camera (decision P): the stop must clear that too.
	[Fact]
	public async Task StopCameraPreviewWhileMediaFoundationInitializesACameraThatCannotBeOpenedKeepsThePreviewStoppedWhenAnotherCameraIsSelected()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			scope.Manager.StopCameraPreview();
			Answer(Assert.Single(mediaFoundation.Initializations), "failsTheReleasedMediaCapture");
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(timeout));

			var other = CreateCamera("fake-start-stop-other-device");
			scope.View.SelectedCamera = other;
			await scope.Manager.UpdateCurrentCamera(other, CancellationToken.None);

			Assert.Single(mediaFoundation.Initializations);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Empty(errors.Errors);
		});
	}

	// A camera switch whose new camera cannot be opened leaves the preview started for the next selected camera (decision P): a stop while the
	// switch waits for Media Foundation must clear that too.
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("opensTheCameraAllTheSame")]
	public async Task StopCameraPreviewWhileACameraSwitchWaitsForMediaFoundationKeepsThePreviewStoppedWhenAnotherCameraIsSelected(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;
			var next = CreateCamera("fake-start-stop-switch-next-device");
			scope.View.SelectedCamera = next;

			var cameraSwitch = scope.Manager.UpdateCurrentCamera(next, CancellationToken.None).AsTask();
			var initialization = Assert.Single(mediaFoundation.Initializations);
			Assert.False(cameraSwitch.IsCompleted, "The camera switch completed without waiting for Media Foundation.");

			scope.Manager.StopCameraPreview();
			Answer(initialization, answer);
			await cameraSwitch.WaitAsync(timeout);

			var other = CreateCamera("fake-start-stop-switch-other-device");
			scope.View.SelectedCamera = other;
			await scope.Manager.UpdateCurrentCamera(other, CancellationToken.None);

			Assert.Single(mediaFoundation.Initializations);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Empty(errors.Errors);
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	// The camera discovery of a real provider waits for Media Foundation as well, before the connection starts the preview.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StopCameraPreviewWhileTheCameraConnectsKeepsThePreviewStoppedWithoutAnError(bool duringTheCameraDiscovery)
	{
		var discovery = new TaskCompletionSource();
		var provider = new WindowsFakeCameraProvider { CamerasAfterRefresh = [CreateCamera("fake-start-stop-connect-device")], Discovery = discovery.Task };

		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			if (!duringTheCameraDiscovery)
			{
				discovery.SetResult();
			}

			var connect = scope.Manager.ConnectCamera(CancellationToken.None);
			Assert.False(connect.IsCompleted, "ConnectCamera completed without waiting for Media Foundation.");
			scope.Manager.StopCameraPreview();
			discovery.TrySetResult();
			foreach (var initialization in mediaFoundation.Initializations)
			{
				Answer(initialization, "failsTheReleasedMediaCapture");
			}

			await connect.WaitAsync(timeout);

			Assert.Equal(duringTheCameraDiscovery ? 0 : 1, mediaFoundation.Initializations.Count);
			Assert.Empty(errors.Errors);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
		}, provider);
	}

	[Theory]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("opensTheCameraAllTheSame")]
	public async Task AStartThatStopCameraPreviewCancelledLeavesTheMediaCaptureOfANewerStartAlone(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var cancelledStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			scope.Manager.StopCameraPreview();
			var newerStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var cancelled = mediaFoundation.Initializations[0];
			var newer = mediaFoundation.Initializations[1];
			Assert.Same(newer.MediaCapture, scope.Access.MediaCapture);

			Answer(cancelled, answer);
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelledStart.WaitAsync(timeout));

			Assert.Same(newer.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(newer.MediaCapture), "The cancelled start released the MediaCapture of the newer start.");
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(cancelled.MediaCapture), "The cancelled start did not release its own MediaCapture.");
			Assert.Empty(errors.Errors);

			newer.Answer.SetResult(false);
			await newerStart.WaitAsync(timeout);

			Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(newer.MediaCapture));
		});
	}

	// Without a stop in between, the newer start owns the preview from the moment it put its MediaCapture in the manager, and releases the
	// MediaCapture of the older start at once: the older start reports nothing when Media Foundation answers.
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AStartThatANewerStartReplacedWhileMediaFoundationInitializedItReleasesOnlyItsOwnMediaCapture(bool opensTheCamera)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var olderStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var newerStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var older = mediaFoundation.Initializations[0];
			var newer = mediaFoundation.Initializations[1];

			older.Answer.SetResult(opensTheCamera);
			await olderStart.WaitAsync(timeout);

			Assert.Same(newer.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(newer.MediaCapture), "The older start released the MediaCapture of the newer start.");
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(older.MediaCapture), "The older start did not release its own MediaCapture.");
			Assert.False(scope.Access.IsInitialized);
			Assert.Equal(0, scope.LoadedCount);
			Assert.Empty(errors.Errors);

			newer.Answer.SetResult(false);
			await newerStart.WaitAsync(timeout);

			Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, Assert.IsType<CameraException>(Assert.Single(errors.Errors)).Message);
			Assert.Null(scope.Access.MediaCapture);
		});
	}

	// The start shows its preview before Media Foundation applies the capture resolution.
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("appliesTheResolutionAllTheSame")]
	public async Task StopCameraPreviewWhileTheConnectionWaitsForMediaFoundationToApplyTheCaptureResolutionKeepsThePreviewStoppedWithoutAnError(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.View.SelectedCamera = CreateCamera("fake-start-stop-resolution-device", WindowsCameraEnvironment.CreateJpegProperties(640, 480));

			var connect = scope.Manager.ConnectCamera(CancellationToken.None);
			var resolution = await OpenTheCameraOfTheLatestStartAsync(mediaFoundation);
			Assert.False(connect.IsCompleted, "ConnectCamera completed without waiting for Media Foundation to apply the capture resolution.");

			scope.Manager.StopCameraPreview();
			AnswerTheResolution(resolution, answer);
			await connect.WaitAsync(timeout);

			Assert.Empty(errors.Errors);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(resolution.MediaCapture), "The MediaCapture of the cancelled start was not released.");
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	// The other platforms show no preview for a start that its token cancelled.
	[Fact]
	public async Task StartCameraPreviewThatItsTokenCancelsWhileMediaFoundationAppliesTheCaptureResolutionStopsThePreviewThatItShows()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.View.SelectedCamera = CreateCamera("fake-start-stop-resolution-device", WindowsCameraEnvironment.CreateJpegProperties(640, 480));
			using var cancellation = new CancellationTokenSource();

			var start = scope.Manager.StartCameraPreview(cancellation.Token);
			var resolution = await OpenTheCameraOfTheLatestStartAsync(mediaFoundation);
			Assert.True(scope.Access.IsInitialized, "The start did not show its preview before Media Foundation applied the capture resolution.");
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(timeout));
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Empty(errors.Errors);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(resolution.MediaCapture), "The cancelled start did not release its MediaCapture.");
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: true);
	}

	// The older start shows its preview while Media Foundation applies the capture resolution: a newer start does nothing then, and the
	// StopCameraPreview that follows cancels the older start, which releases its own MediaCapture.
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("appliesTheResolutionAllTheSame")]
	public async Task AStartWhileAnOlderStartWaitsForMediaFoundationToApplyTheCaptureResolutionDoesNothingAndStopCameraPreviewReleasesTheOlderMediaCapture(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.View.SelectedCamera = CreateCamera("fake-start-stop-resolution-device", WindowsCameraEnvironment.CreateJpegProperties(640, 480));

			var olderStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var olderResolution = await OpenTheCameraOfTheLatestStartAsync(mediaFoundation);
			await scope.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(timeout);

			Assert.Single(mediaFoundation.Initializations);
			Assert.Same(olderResolution.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(olderResolution.MediaCapture), "The newer start released the MediaCapture of the preview that shows.");

			scope.Manager.StopCameraPreview();
			AnswerTheResolution(olderResolution, answer);

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => olderStart.WaitAsync(timeout));
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(olderResolution.MediaCapture), "The older start did not release its own MediaCapture.");
			Assert.Empty(errors.Errors);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	// Media Foundation can fail the resolution of the MediaCapture that a camera switch or the release of the view released.
	[Theory]
	[InlineData("aCameraSwitch")]
	[InlineData("theReleaseOfTheView")]
	public async Task AStartWhoseMediaCaptureWasReleasedWhileMediaFoundationAppliedTheCaptureResolutionReportsNothing(string releasedBy)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.View.SelectedCamera = CreateCamera("fake-start-stop-resolution-device", WindowsCameraEnvironment.CreateJpegProperties(640, 480));

			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var resolution = await OpenTheCameraOfTheLatestStartAsync(mediaFoundation);

			Task? cameraSwitch = null;
			if (releasedBy is "aCameraSwitch")
			{
				var other = CreateCamera("fake-start-stop-switch-device");
				scope.View.SelectedCamera = other;
				cameraSwitch = scope.Manager.UpdateCurrentCamera(other, CancellationToken.None).AsTask();
			}
			else
			{
				scope.Manager.Dispose();
			}

			resolution.Answer.SetException(new ObjectDisposedException("MediaCapture"));
			await start.WaitAsync(timeout);

			Assert.Empty(errors.Errors);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(resolution.MediaCapture), "The MediaCapture of the start was not released.");
			Assert.Equal(0, scope.LoadedCount);

			if (cameraSwitch is not null)
			{
				// The switch still waits for Media Foundation to open the other camera
				scope.Manager.StopCameraPreview();
				await cameraSwitch.WaitAsync(timeout);
				Assert.Empty(errors.Errors);
			}
		}, observesCancellation: true);
	}

	// The token of the start is linked with the token that StopCameraPreview cancels: the exception still has the type of the platform's
	// cancellation and names the caller's token.
	[Fact]
	public async Task StartCameraPreviewThatItsTokenCancelsWhileMediaFoundationInitializesTheCameraThrowsWithThatToken()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var cancellation = new CancellationTokenSource();

			var start = scope.Manager.StartCameraPreview(cancellation.Token);
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(timeout));
			Assert.IsType<TaskCanceledException>(exception);
			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.Empty(errors.Errors);
			Assert.Null(scope.Access.MediaCapture);
		}, observesCancellation: true);
	}

	// The preview counts as started with a MediaCapture that was never initialized, which only the scripted preparation of the recording uses.
	// Media Foundation can observe the cancellation of the preparation, fail it for the MediaCapture that the stop released, or complete it all
	// the same (the scripted answer has no recording to return: only a real camera prepares one).
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("preparesTheRecordingAllTheSame")]
	public async Task StopCameraPreviewWhileMediaFoundationPreparesARecordingCancelsTheRecording(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var recorder = new WindowsMediaRecorder(scope.View);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var preparation = Assert.Single(mediaFoundation.RecordingPreparations);
			Assert.False(recording.IsCompleted, "StartVideoRecording completed without waiting for Media Foundation.");
			scope.Manager.StopCameraPreview();
			if (answer is "failsTheReleasedMediaCapture")
			{
				preparation.Answer.SetException(new ObjectDisposedException("MediaCapture"));
			}
			else if (answer is "preparesTheRecordingAllTheSame")
			{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
				preparation.Answer.SetResult(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			}

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));
			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the recording.");
			Assert.Null(scope.Access.MediaRecording);
			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Empty(recorder.Failures);
			Assert.Empty(errors.Errors);
			GC.KeepAlive(recorder);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	[Fact]
	public async Task StopCameraPreviewCommandWhileTheStartCameraPreviewCommandWaitsForMediaFoundationRaisesNoErrorAndLetsNothingEscape()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			var view = new CameraView { SelectedCamera = CreateCamera("fake-start-stop-command-device") };
			var handler = CreateHandler<WindowsNoConnectCameraViewHandler>(view);
			var access = new WindowsCameraManagerAccessor(handler.CameraManager);
			var mediaFoundation = new WindowsScriptedMediaFoundation();
			mediaFoundation.ReplaceTheMediaFoundationOf(access);
			using var errors = new CameraErrorRecorder(view);
			var context = new WindowsRecordingSynchronizationContext();

			try
			{
				using (context.Install())
				{
					view.StartCameraPreviewCommand.Execute(CancellationToken.None);
					view.StopCameraPreviewCommand.Execute(null);
				}

				Answer(Assert.Single(mediaFoundation.Initializations), "failsTheReleasedMediaCapture");
				var unhandled = await context.DrainUntilAsyncVoidMethodsEndAsync(timeout);

				Assert.Empty(unhandled);
				Assert.Empty(errors.Errors);
				Assert.False(access.IsInitialized);
				Assert.Null(access.MediaCapture);
			}
			finally
			{
				view.Handler?.DisconnectHandler();
				view.Handler = null;
				view.Dispose();
			}
		});
	}

	// A second MediaCapture would replace the one that shows the preview, and nothing would release the first one.
	[Fact]
	public async Task StartCameraPreviewWhileThePreviewRunsChangesNothing()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var firstStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var first = Assert.Single(mediaFoundation.Initializations);
			first.Answer.SetResult(true);
			await firstStart.WaitAsync(timeout);
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(1, scope.LoadedCount);

			await scope.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(timeout);

			Assert.Single(mediaFoundation.Initializations);
			Assert.Same(first.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(first.MediaCapture), "The second start released the MediaCapture of the running preview.");
			Assert.True(scope.Access.IsInitialized);
			Assert.Equal(1, scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	// Media Foundation answers a camera that hangs never, and nothing else would release the MediaCapture of the older start.
	[Fact]
	public async Task AStartThatReplacesAStartThatStillWaitsForMediaFoundationReleasesTheMediaCaptureOfTheOlderStartAtOnce()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var olderStart = scope.Manager.StartCameraPreview(CancellationToken.None);
			var older = Assert.Single(mediaFoundation.Initializations);

			var newerStart = scope.Manager.StartCameraPreview(CancellationToken.None);

			var newer = mediaFoundation.Initializations[1];
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(older.MediaCapture), "The newer start did not release the MediaCapture of the older start before Media Foundation answered it.");
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(newer.MediaCapture));
			Assert.Same(newer.MediaCapture, scope.Access.MediaCapture);

			older.Answer.SetResult(true);
			await olderStart.WaitAsync(timeout);
			newer.Answer.SetResult(true);
			await newerStart.WaitAsync(timeout);

			Assert.Same(newer.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(newer.MediaCapture));
			Assert.Equal(1, scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	// Releasing the MediaCapture ends its recording, like unbinding the use cases on Android. The scripted preparation has no LowLagMediaRecording,
	// so the stream is the part of the recording that is observable: the stop forgets it, and a preview that starts again does not find it.
	[Fact]
	public async Task StopCameraPreviewDuringARecordingForgetsTheStreamOfTheRecording()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var recorder = new WindowsMediaRecorder(scope.View);
			scope.Access.MediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			scope.Access.IsInitialized = true;
			using var destination = new MemoryStream();
			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);

			// Only a real camera prepares a recording: the scripted answer has none, and the stream is the part of the recording that is observable
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
			Assert.Single(mediaFoundation.RecordingPreparations).Answer.SetResult(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
			await recording.WaitAsync(timeout);
			Assert.Same(destination, scope.Access.VideoCaptureStream);

			scope.Manager.StopCameraPreview();

			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Null(scope.Access.MediaRecording);

			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			mediaFoundation.Initializations[^1].Answer.SetResult(true);
			await start.WaitAsync(timeout);

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));
			Assert.Empty(recorder.Failures);
			Assert.Empty(errors.Errors);
			GC.KeepAlive(recorder);
		});
	}

	// Like on Android, iOS and Mac Catalyst, a recording on a stopped preview turns the camera on. The scripted preparation has no
	// LowLagMediaRecording, so the stream is the part of the recording that is observable.
	[Fact]
	public async Task StartVideoRecordingOnAStoppedPreviewTurnsTheCameraOnAndPreparesTheRecording()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();
			Assert.False(scope.Access.IsInitialized);

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			Assert.False(recording.IsCompleted, "StartVideoRecording completed without waiting for Media Foundation to open the camera.");
			Assert.Empty(mediaFoundation.RecordingPreparations);

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			var preparation = Assert.Single(mediaFoundation.RecordingPreparations);
			Assert.Same(initialization.MediaCapture, preparation.MediaCapture);
			Assert.False(recording.IsCompleted, "StartVideoRecording completed without waiting for Media Foundation to prepare the recording.");

			PrepareWithoutARecording(preparation);
			await recording.WaitAsync(timeout);

			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.Same(destination, scope.Access.VideoCaptureStream);
			Assert.Equal(1, scope.LoadedCount);
			Assert.Empty(errors.Errors);
		});
	}

	// A start that cannot turn the camera on throws the failure instead of raising ErrorOccurred (the command of the view reports it once), and a
	// stopped preview stays stopped: the preview alone would count a camera that cannot be opened as started.
	[Theory]
	[InlineData("cannotBeOpened")]
	[InlineData("accessIsDenied")]
	public async Task StartVideoRecordingOnAStoppedPreviewWhoseCameraCannotBeTurnedOnThrowsAndLeavesThePreviewStopped(string failure)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			if (failure is "accessIsDenied")
			{
				initialization.Answer.SetException(new UnauthorizedAccessException("The camera is blocked by the privacy settings."));
			}
			else
			{
				initialization.Answer.SetResult(false);
			}

			if (failure is "accessIsDenied")
			{
				await Assert.ThrowsAsync<UnauthorizedAccessException>(() => recording.WaitAsync(timeout));
			}
			else
			{
				var exception = await Assert.ThrowsAsync<CameraException>(() => recording.WaitAsync(timeout));
				Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, exception.Message);
			}

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The MediaCapture of the failed start was not released.");
			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Empty(mediaFoundation.RecordingPreparations);
			Assert.Empty(errors.Errors);
			Assert.Equal(0, scope.LoadedCount);

			// The failed recording left no state behind: a start of the preview opens the camera again
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			Assert.Equal(2, mediaFoundation.Initializations.Count);
			mediaFoundation.Initializations[^1].Answer.SetResult(true);
			await start.WaitAsync(timeout);
			Assert.True(scope.Access.IsInitialized);
		});
	}

	[Fact]
	public async Task StartVideoRecordingOnAStoppedPreviewWithoutACameraThrowsAndLeavesThePreviewStopped()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var exception = await Assert.ThrowsAsync<CameraException>(() => scope.Manager.StartVideoRecording(destination, CancellationToken.None).WaitAsync(timeout));

			Assert.Equal("Couldn't start camera preview; no cameras available", exception.Message);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Empty(mediaFoundation.Initializations);
			Assert.Empty(errors.Errors);
		}, new WindowsFakeCameraProvider());
	}

	// ICameraView documents OperationCanceledException for a start whose token is cancelled, also when the start of the camera would fail first.
	[Fact]
	public async Task StartVideoRecordingWithACancelledTokenOnAStoppedPreviewWithoutACameraThrowsOperationCanceledException()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();
			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => scope.Manager.StartVideoRecording(destination, cancellation.Token).WaitAsync(timeout));

			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Empty(mediaFoundation.Initializations);
			Assert.Empty(errors.Errors);
		}, new WindowsFakeCameraProvider());
	}

	// A preview that the app started, whose camera could not be opened, counts as started (selecting another camera starts it): a recording that
	// fails to open the camera again keeps that, like on iOS and Mac Catalyst, where such a preview stays started.
	[Fact]
	public async Task StartVideoRecordingOnAStartedPreviewWithoutAMediaCaptureThatCannotBeTurnedOnKeepsThePreviewStarted()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.Access.IsInitialized = true;
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			Assert.Single(mediaFoundation.Initializations).Answer.SetResult(false);

			await Assert.ThrowsAsync<CameraException>(() => recording.WaitAsync(timeout));

			Assert.True(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.Empty(errors.Errors);
		});
	}

	// The cancellation of the camera that the recording turns on is the cancellation of the start of the preview: the exception names the token
	// that cancelled, the camera is released and nothing is reported.
	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("opensTheCameraAllTheSame")]
	public async Task StartVideoRecordingThatItsTokenCancelsWhileMediaFoundationOpensTheCameraThrowsAndLeavesThePreviewStopped(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var cancellation = new CancellationTokenSource();
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, cancellation.Token);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			cancellation.Cancel();
			Answer(initialization, answer);

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));

			Assert.Equal(cancellation.Token, exception.CancellationToken);
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The MediaCapture of the cancelled start was not released.");
			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Empty(mediaFoundation.RecordingPreparations);
			Assert.Empty(errors.Errors);
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	[Theory]
	[InlineData("observesTheCancellation")]
	[InlineData("failsTheReleasedMediaCapture")]
	[InlineData("opensTheCameraAllTheSame")]
	public async Task StopCameraPreviewWhileMediaFoundationOpensTheCameraForARecordingCancelsTheRecording(string answer)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			scope.Manager.StopCameraPreview();
			Answer(initialization, answer);

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));

			Assert.True(exception.CancellationToken.IsCancellationRequested, "The exception does not name the token that cancelled the recording.");
			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The MediaCapture of the cancelled start was not released.");
			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Empty(mediaFoundation.RecordingPreparations);
			Assert.Empty(errors.Errors);
			Assert.Equal(0, scope.LoadedCount);
		}, observesCancellation: answer is "observesTheCancellation");
	}

	// The camera that the recording turned on is off again when the recording cannot be prepared, whether the token cancelled the preparation
	// or Media Foundation failed it.
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task StartVideoRecordingOnAStoppedPreviewTurnsTheCameraOffAgainWhenTheRecordingCannotBePrepared(bool cancelledByTheToken)
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var cancellation = new CancellationTokenSource();
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, cancellation.Token);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			Assert.True(scope.Access.IsInitialized);

			if (cancelledByTheToken)
			{
				cancellation.Cancel();
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => recording.WaitAsync(timeout));
			}
			else
			{
				Assert.Single(mediaFoundation.RecordingPreparations).Answer.SetException(new InvalidOperationException("Media Foundation cannot prepare the recording."));
				await Assert.ThrowsAsync<InvalidOperationException>(() => recording.WaitAsync(timeout));
			}

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The MediaCapture that the recording opened was not released.");
			Assert.Null(scope.Access.VideoCaptureStream);
			Assert.Empty(errors.Errors);
		}, observesCancellation: cancelledByTheToken);
	}

	// A preview that the app started is not the recording's to turn off, and the recording does not open the camera a second time.
	[Fact]
	public async Task StartVideoRecordingOnAStartedPreviewThatCannotBePreparedKeepsThePreviewStarted()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await start.WaitAsync(timeout);
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			Assert.Single(mediaFoundation.RecordingPreparations).Answer.SetException(new InvalidOperationException("Media Foundation cannot prepare the recording."));
			await Assert.ThrowsAsync<InvalidOperationException>(() => recording.WaitAsync(timeout));

			Assert.Single(mediaFoundation.Initializations);
			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The failed recording released the camera of the preview that the app started.");
			Assert.Empty(errors.Errors);
		});
	}

	// A second start joins the first, as on Android, iOS and Mac Catalyst: the camera opens once, the recording is prepared once, and the stream of
	// the first start receives it.
	[Fact]
	public async Task ASecondStartVideoRecordingWhileTheFirstTurnsTheCameraOnJoinsIt()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var firstDestination = new MemoryStream();
			using var secondDestination = new MemoryStream();

			var first = scope.Manager.StartVideoRecording(firstDestination, CancellationToken.None);
			var second = scope.Manager.StartVideoRecording(secondDestination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			Assert.False(second.IsCompleted, "The second StartVideoRecording completed without waiting for the first.");

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			var preparation = Assert.Single(mediaFoundation.RecordingPreparations);
			PrepareWithoutARecording(preparation);

			await Task.WhenAll(first, second).WaitAsync(timeout);

			Assert.Single(mediaFoundation.Initializations);
			Assert.Same(firstDestination, scope.Access.VideoCaptureStream);
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task ASecondStartVideoRecordingThrowsTheFailureOfTheFirst()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var firstDestination = new MemoryStream();
			using var secondDestination = new MemoryStream();

			var first = scope.Manager.StartVideoRecording(firstDestination, CancellationToken.None);
			var second = scope.Manager.StartVideoRecording(secondDestination, CancellationToken.None);
			Assert.Single(mediaFoundation.Initializations).Answer.SetResult(false);

			var firstException = await Assert.ThrowsAsync<CameraException>(() => first.WaitAsync(timeout));
			var secondException = await Assert.ThrowsAsync<CameraException>(() => second.WaitAsync(timeout));

			Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, firstException.Message);
			Assert.Equal(CameraManagerWindowsTests.CameraThatCannotBeOpenedMessage, secondException.Message);
			Assert.Single(mediaFoundation.Initializations);
			Assert.Empty(errors.Errors);
		});
	}

	// A stop during the camera-on waits for the start, instead of returning at once and leaving the camera recording into the stream of the app.
	[Fact]
	public async Task StopVideoRecordingWhileStartVideoRecordingTurnsTheCameraOnWaitsForTheStart()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			var stop = scope.Manager.StopVideoRecording(CancellationToken.None);
			Assert.False(stop.IsCompleted, "StopVideoRecording completed without waiting for the start that turns the camera on.");

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));

			await recording.WaitAsync(timeout);
			Assert.Same(Stream.Null, await stop.WaitAsync(timeout));

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The camera that the recording turned on is still on.");
			Assert.Empty(errors.Errors);
		});
	}

	// Media Foundation opened the camera and failed what came after it: the preview that the app stopped stays stopped, as on iOS and Mac Catalyst.
	[Fact]
	public async Task StartVideoRecordingOnAStoppedPreviewWhoseCaptureResolutionCannotBeAppliedThrowsAndLeavesThePreviewStopped()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			scope.View.SelectedCamera = CreateCamera("fake-start-stop-resolution-device", WindowsCameraEnvironment.CreateJpegProperties(640, 480));
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var resolution = await OpenTheCameraOfTheLatestStartAsync(mediaFoundation);
			resolution.Answer.SetException(new InvalidOperationException("Media Foundation cannot apply the capture resolution."));

			await Assert.ThrowsAsync<InvalidOperationException>(() => recording.WaitAsync(timeout));

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(resolution.MediaCapture), "The camera of the failed start is still on.");
			Assert.Empty(mediaFoundation.RecordingPreparations);
			Assert.Empty(errors.Errors);
			Assert.Equal(0, scope.LoadedCount);
		});
	}

	// The recording waits for the start of the preview that the app made, and records with that preview, which stays started.
	[Fact]
	public async Task StartVideoRecordingWhileAStartOfThePreviewWaitsForMediaFoundationRecordsWithThatPreviewAndKeepsItStarted()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));

			await Task.WhenAll(start, recording).WaitAsync(timeout);
			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.Single(mediaFoundation.Initializations);
			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The recording turned off the preview that the app started.");
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task StartCameraPreviewWhileAStartOfARecordingTurnsTheCameraOnWaitsForItAndKeepsThePreviewStarted()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));

			await Task.WhenAll(recording, start).WaitAsync(timeout);
			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.Single(mediaFoundation.Initializations);
			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.Empty(errors.Errors);
		});
	}

	// The restart of a camera switch keeps a camera that only the recording turned on, and the StopVideoRecording that finds no recording turns it off.
	[Fact]
	public async Task ACameraSwitchDuringARecordingThatTurnedTheCameraOnTurnsTheCameraOffWhenStopVideoRecordingEndsTheRecording()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();
			var other = CreateCamera("fake-start-stop-switch-device");

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			mediaFoundation.Initializations[0].Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			scope.View.SelectedCamera = other;
			var cameraSwitch = scope.Manager.UpdateCurrentCamera(other, CancellationToken.None).AsTask();
			var restartedInitialization = mediaFoundation.Initializations[^1];
			restartedInitialization.Answer.SetResult(true);
			await cameraSwitch.WaitAsync(timeout);
			Assert.True(scope.Access.IsInitialized);

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(restartedInitialization.MediaCapture), "The camera that only the recording turned on is still on.");
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task StopVideoRecordingAfterARecordingThatTurnedTheCameraOnTurnsTheCameraOffAgain()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The camera that the recording turned on is still on.");
			Assert.Empty(errors.Errors);
		});
	}

	// Before the stop waited for nothing that was complete, a token that was cancelled already made the stop throw without stopping, and the
	// camera that only the recording turned on stayed on
	[Fact]
	public async Task StopVideoRecordingWithACancelledTokenAfterARecordingThatTurnedTheCameraOnStillTurnsTheCameraOff()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();
			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			using var cancellation = new CancellationTokenSource();
			cancellation.Cancel();

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(cancellation.Token).WaitAsync(timeout));

			Assert.False(scope.Access.IsInitialized);
			Assert.Null(scope.Access.MediaCapture);
			Assert.True(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The camera that the recording turned on is still on.");
			Assert.Empty(errors.Errors);
		});
	}

	// The token of a stop still cancels the wait for a start that is pending: the start goes on and records
	[Fact]
	public async Task StopVideoRecordingWhoseTokenCancelsWhileItWaitsForAStartThrowsAndTheStartStillRecords()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();
			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);

			using var cancellation = new CancellationTokenSource();
			var stop = scope.Manager.StopVideoRecording(cancellation.Token);
			cancellation.Cancel();

			var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => stop.WaitAsync(timeout));
			Assert.Equal(cancellation.Token, exception.CancellationToken);

			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			Assert.True(scope.Access.IsInitialized);
			Assert.Same(destination, scope.Access.VideoCaptureStream);

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));
			Assert.False(scope.Access.IsInitialized);
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task StopVideoRecordingAfterARecordingOnAStartedPreviewKeepsThePreviewStarted()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			var start = scope.Manager.StartCameraPreview(CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await start.WaitAsync(timeout);
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The stop turned off the preview that the app started.");
			Assert.Empty(errors.Errors);
		});
	}

	[Fact]
	public async Task StartCameraPreviewDuringARecordingThatTurnedTheCameraOnKeepsThePreviewStartedAfterStopVideoRecording()
	{
		await RunAsync(async (scope, mediaFoundation, errors) =>
		{
			using var destination = new MemoryStream();

			var recording = scope.Manager.StartVideoRecording(destination, CancellationToken.None);
			var initialization = Assert.Single(mediaFoundation.Initializations);
			initialization.Answer.SetResult(true);
			await AssertHelpers.AssertEventually(() => mediaFoundation.RecordingPreparations.Count > 0, (int)timeout.TotalMilliseconds,
				message: "The recording was not prepared once Media Foundation opened the camera.");
			PrepareWithoutARecording(Assert.Single(mediaFoundation.RecordingPreparations));
			await recording.WaitAsync(timeout);

			await scope.Manager.StartCameraPreview(CancellationToken.None).WaitAsync(timeout);
			Assert.Same(Stream.Null, await scope.Manager.StopVideoRecording(CancellationToken.None).WaitAsync(timeout));

			Assert.Single(mediaFoundation.Initializations);
			Assert.True(scope.Access.IsInitialized);
			Assert.Same(initialization.MediaCapture, scope.Access.MediaCapture);
			Assert.False(await WindowsCameraEnvironment.IsClosedAsync(initialization.MediaCapture), "The stop turned off the preview that the app started.");
			Assert.Empty(errors.Errors);
		});
	}

	// A start releases its own MediaCapture also when StopCameraPreview, a camera switch or the release of the view released it before.
	[Fact]
	public async Task AMediaCaptureCanBeReleasedTwice()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			var mediaCapture = WindowsCameraEnvironment.CreateMediaCapture();
			mediaCapture.Dispose();

			Assert.Null(Record.Exception(mediaCapture.Dispose));
		});
	}

	// The scripted preparation has no LowLagMediaRecording to hand over
	static void PrepareWithoutARecording(WindowsScriptedMediaFoundation.Request<LowLagMediaRecording> preparation)
	{
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		preparation.Answer.SetResult(null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	static void Answer(WindowsScriptedMediaFoundation.Request<bool> initialization, string answer)
	{
		switch (answer)
		{
			case "observesTheCancellation":
				Assert.True(initialization.Answer.Task.IsCanceled, "The scripted Media Foundation did not observe the cancellation.");
				break;
			case "failsTheReleasedMediaCapture":
				initialization.Answer.TrySetResult(false);
				break;
			case "opensTheCameraAllTheSame":
				initialization.Answer.TrySetResult(true);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(answer), answer, null);
		}
	}

	static void AnswerTheResolution(WindowsScriptedMediaFoundation.Request<bool> resolution, string answer)
	{
		switch (answer)
		{
			case "observesTheCancellation":
				Assert.True(resolution.Answer.Task.IsCanceled, "The scripted Media Foundation did not observe the cancellation.");
				break;
			case "failsTheReleasedMediaCapture":
				resolution.Answer.TrySetException(new ObjectDisposedException("MediaCapture"));
				break;
			case "appliesTheResolutionAllTheSame":
				resolution.Answer.TrySetResult(true);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(answer), answer, null);
		}
	}

	static async Task<WindowsScriptedMediaFoundation.Request<bool>> OpenTheCameraOfTheLatestStartAsync(WindowsScriptedMediaFoundation mediaFoundation)
	{
		var resolutionChanges = mediaFoundation.ResolutionChanges.Count;

		mediaFoundation.Initializations[^1].Answer.SetResult(true);

		await AssertHelpers.AssertEventually(() => mediaFoundation.ResolutionChanges.Count > resolutionChanges, (int)timeout.TotalMilliseconds,
			message: "The start did not apply the capture resolution once Media Foundation opened the camera.");

		return mediaFoundation.ResolutionChanges[^1];
	}

	static CameraInfo CreateCamera(string deviceId, params ImageEncodingProperties[] photoResolutions)
	{
		// The scripted Media Foundation answers for a camera that no device has, so it has no frame source group
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		return new CameraInfo("Scripted camera", deviceId, CameraPosition.Rear, false, 1f, 1f, [], null, photoResolutions);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
	}

	async Task RunAsync(Func<WindowsCameraManagerScope, WindowsScriptedMediaFoundation, CameraErrorRecorder, Task> test, WindowsFakeCameraProvider? provider = null, bool observesCancellation = false)
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(async () =>
		{
			using var scope = new WindowsCameraManagerScope(MauiContext, provider ?? new WindowsFakeCameraProvider { AvailableCameras = [CreateCamera("fake-start-stop-device")] });
			using var errors = new CameraErrorRecorder(scope.View);
			scope.Manager.CreatePlatformView();
			var mediaFoundation = new WindowsScriptedMediaFoundation { ObservesCancellation = observesCancellation };
			mediaFoundation.ReplaceTheMediaFoundationOf(scope.Access);

			await test(scope, mediaFoundation, errors);
		});
	}
}