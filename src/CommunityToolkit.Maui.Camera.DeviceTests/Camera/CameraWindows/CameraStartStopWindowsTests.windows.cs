using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.DeviceTests;

// The test machines have no camera, so WindowsScriptedMediaFoundation answers the initializations and recording preparations of the manager
// when the test says so: a StopCameraPreview, or another start, lands while a start waits for Media Foundation.
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

	// Without a stop in between, the newer start owns the preview from the moment it put its MediaCapture in the manager: the older start
	// releases its own MediaCapture when Media Foundation answers, and reports nothing.
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

	static CameraInfo CreateCamera(string deviceId)
	{
		// The scripted Media Foundation answers for a camera that no device has, so it has no frame source group
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
		return new CameraInfo("Scripted camera", deviceId, CameraPosition.Rear, false, 1f, 1f, [], null, []);
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
