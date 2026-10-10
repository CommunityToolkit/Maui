using System.Text;
using AndroidX.Camera.Core;
using AndroidX.Camera.Extensions;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Views;
using CameraInfo = CommunityToolkit.Maui.Core.CameraInfo;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.CameraViewAndroid)]
[Collection(TestCollections.DisableParallelization)]
public class CameraViewAndroidTests : CameraAndroidTestBase
{
	static async Task<byte[]> CaptureJpegAsync(CameraView view)
	{
		using var cancellation = new CancellationTokenSource(CameraTimeout);
		var stream = await view.CaptureImage(cancellation.Token);
		var jpeg = await ReadAllBytesAsync(stream);
		Assert.True(IsJpeg(jpeg), "The captured stream is not a JPEG.");
		return jpeg;
	}

	static void AssertIsAPlayableVideo(byte[] mp4)
	{
		Assert.True(mp4.Length > 8 && Encoding.ASCII.GetString(mp4, 4, 4) == "ftyp", "The stream does not start with an MP4 'ftyp' box.");
		var (hasVideo, durationMilliseconds, width, height) = ReadVideoInfo(mp4);
		Assert.True(hasVideo, "Android does not find a video track in the recording.");
		Assert.True(durationMilliseconds > 0, "The recording has no duration.");
		Assert.True(width > 0 && height > 0, $"The recording has no size ({width}x{height}).");
	}

	static Task WaitUntilAsync(Func<bool> condition, string message) => AssertionExtensions.AssertEventually(condition, 30000, 50, message);

	static object CreateImageCallbackOf(CameraView view)
	{
		var type = typeof(CameraManager).GetNestedType("ImageCallBack", System.Reflection.BindingFlags.NonPublic) ?? throw new MissingMemberException("CameraManager.ImageCallBack");
		return Activator.CreateInstance(type, view)!;
	}

	static Task WaitForRecordingDataAsync(IReadOnlyCollection<string> before) =>
		WaitUntilAsync(() => ListRecordingFiles().Except(before).SingleOrDefault() is string path && new FileInfo(path).Length > 0, "The recording never wrote any data into the cache directory.");

	[Fact]
	public async Task SetExtensionModeBindsTheUseCasesOfAHostedViewAgain()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var preview = host.Native<Preview>("cameraPreview")!;
			var imageCapture = host.Native<ImageCapture>("imageCapture")!;
			await UnbindAllNativeAsync();
			Assert.False(await IsNativelyBoundAsync(preview));
			Assert.False(await IsNativelyBoundAsync(imageCapture));

			await host.View.SetExtensionMode(ExtensionMode.None).WaitAsync(CameraTimeout);

			Assert.True(await IsNativelyBoundAsync(preview));
			Assert.True(await IsNativelyBoundAsync(imageCapture));
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
		});
	}

	[Fact]
	public async Task SetExtensionModeWithoutAHandlerThrowsInvalidOperationException()
	{
		SetupBuilder();
		var view = new CameraView();

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => view.SetExtensionMode(ExtensionMode.None));

		Assert.Equal("Unable to retrieve Handler", exception.Message);
	}

	[Fact]
	public async Task SetExtensionModeWithACancelledTokenThrowsOperationCanceledException()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.View.SetExtensionMode(ExtensionMode.None, cancellation.Token));

			Assert.True(await IsNativelyBoundAsync(host.Native<ImageCapture>("imageCapture")!));
		});
	}

	// SetExtensionMode binds the camera again, and CameraX starts a bound camera with its defaults.
	[Fact]
	public async Task ZoomFactorOfTheViewStillDescribesTheNativeZoomAfterSetExtensionMode()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "No camera of this device can zoom in.");
		var ratio = Math.Min(2f, camera.MaximumZoomFactor);

		await RunWithCameraViewAsync(async host =>
		{
			host.View.ZoomFactor = ratio;
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);

			await RunAndWaitForANewPreviewStreamAsync(host.PreviewView, () => host.View.SetExtensionMode(ExtensionMode.None).WaitAsync(CameraTimeout));

			// Either the zoom is kept or the view is reset: the two must agree.
			await WaitForNativeZoomAsync(camera.DeviceId, host.View.ZoomFactor,
				message: $"After SetExtensionMode the native zoom ratio of camera {camera.DeviceId} is not CameraView.ZoomFactor ({host.View.ZoomFactor}).");
		}, new CameraView { SelectedCamera = camera });
	}

	[Fact]
	public async Task TorchOfTheViewStillDescribesTheNativeTorchAfterSetExtensionMode()
	{
		SetupBuilder();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.IsFlashSupported);
		Skip.If(camera is null, "No camera of this device has a flash unit, so the torch can not be switched on.");

		await RunWithCameraViewAsync(async host =>
		{
			host.View.IsTorchOn = true;
			await WaitForNativeTorchAsync(camera.DeviceId, true);

			await RunAndWaitForANewPreviewStreamAsync(host.PreviewView, () => host.View.SetExtensionMode(ExtensionMode.None).WaitAsync(CameraTimeout));

			// Either the torch is kept or the view is reset: the two must agree.
			await WaitForNativeTorchAsync(camera.DeviceId, host.View.IsTorchOn,
				message: $"After SetExtensionMode the native torch of camera {camera.DeviceId} does not match CameraView.IsTorchOn ({host.View.IsTorchOn}).");
		}, new CameraView { SelectedCamera = camera });
	}

	sealed class ViewThatDisposesWithoutDisposing : CameraView
	{
		public void DisposeWithoutDisposing() => Dispose(false);
	}

	[Fact]
	public async Task CaptureImageAfterDisposeThrowsObjectDisposedException()
	{
		SetupBuilder();
		var view = new CameraView();

		view.Dispose();

		// Disposing the view disposed the semaphore that serializes the captures.
		await Assert.ThrowsAsync<ObjectDisposedException>(() => view.CaptureImage(CancellationToken.None));
	}

	[Fact]
	public void DisposingTheViewTwiceIsSafe()
	{
		SetupBuilder();
		var view = new CameraView();
		view.Dispose();

		var exception = Record.Exception(view.Dispose);

		Assert.Null(exception);
	}

	[Fact]
	public async Task DisposeWithoutDisposingKeepsTheCaptureSemaphoreAndLaterDisposeCallsDoNothing()
	{
		SetupBuilder();
		var view = new ViewThatDisposesWithoutDisposing();

		view.DisposeWithoutDisposing();
		view.Dispose();

		// The semaphore was never disposed (Dispose(false), then Dispose of a view that counts as disposed): CaptureImage gets past
		// it and only fails because the view has no handler.
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => view.CaptureImage(CancellationToken.None));
		Assert.Equal("Unable to retrieve Handler", exception.Message);
	}

	[Fact]
	public async Task GetAvailableCamerasReturnsTheCamerasOfTheDeviceInTheNativeOrder()
	{
		SetupBuilder();
		var view = new CameraView();

		var cameras = await view.GetAvailableCameras(CancellationToken.None).AsTask().WaitAsync(CameraTimeout);

		var nativeIds = (await GetNativeCameraProviderAsync()).AvailableCameraInfos.Select(GetNativeCameraId).ToList();
		Assert.NotEmpty(nativeIds);
		Assert.Equal(nativeIds, cameras.Select(camera => camera.DeviceId).ToList());
	}

	[Fact]
	public async Task GetAvailableCamerasRefreshesTheProviderThatHasNoCamerasYet()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = null, CamerasAfterRefresh = cameras };
		UseCameraProvider(provider);

		var available = await new CameraView().GetAvailableCameras(CancellationToken.None);

		Assert.Same(cameras, available);
		Assert.Equal(1, provider.RefreshCount);
	}

	[Fact]
	public async Task GetAvailableCamerasDoesNotRefreshAProviderThatKnowsTheCameras()
	{
		SetupBuilder();
		var cameras = await GetRealCamerasAsync();
		var provider = new StubCameraProvider { AvailableCameras = cameras };
		UseCameraProvider(provider);

		var available = await new CameraView().GetAvailableCameras(CancellationToken.None);

		Assert.Same(cameras, available);
		Assert.Equal(0, provider.RefreshCount);
	}

	[Fact]
	public async Task GetAvailableCamerasReturnsAnEmptyListWhenTheProviderKnowsNoCameras()
	{
		SetupBuilder();
		UseCameraProvider(new StubCameraProvider { AvailableCameras = [] });

		var available = await new CameraView().GetAvailableCameras(CancellationToken.None);

		Assert.Empty(available);
	}

	[Fact]
	public async Task GetAvailableCamerasThrowsCameraExceptionWhenTheRefreshFindsNothing()
	{
		SetupBuilder();
		var provider = new StubCameraProvider { AvailableCameras = null, CamerasAfterRefresh = null };
		UseCameraProvider(provider);

		var exception = await Assert.ThrowsAsync<CameraException>(async () => await new CameraView().GetAvailableCameras(CancellationToken.None));

		Assert.Equal("No camera available on device", exception.Message);
		Assert.Equal(1, provider.RefreshCount);
	}

	[Fact]
	public async Task GetAvailableCamerasWithACancelledTokenThrowsOperationCanceledExceptionFromTheProvider()
	{
		SetupBuilder();
		var provider = new StubCameraProvider { AvailableCameras = null, CamerasAfterRefresh = await GetRealCamerasAsync() };
		UseCameraProvider(provider);
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await new CameraView().GetAvailableCameras(cancellation.Token));
	}

	[Fact]
	public async Task GetAvailableCamerasThrowsCameraExceptionWhenThereIsNoPlatformApplication()
	{
		SetupBuilder();
		var previous = IPlatformApplication.Current;
		IPlatformApplication.Current = null;

		try
		{
			var exception = await Assert.ThrowsAsync<CameraException>(async () => await new CameraView().GetAvailableCameras(CancellationToken.None));

			Assert.Equal("Unable to retrieve CameraProvider", exception.Message);
		}
		finally
		{
			IPlatformApplication.Current = previous;
		}
	}

	[Fact]
	public async Task CaptureImageReturnsAJpegOfTheNativeCaptureResolution()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			var jpeg = await CaptureJpegAsync(host.View);

			var resolution = GetNativeResolution(host.Native<ImageCapture>("imageCapture")!.ResolutionInfo);
			Assert.Equal(resolution, GetJpegSize(jpeg));
		});
	}

	[Fact]
	public async Task CaptureImageReturnsTheSameStreamThatTheMediaCapturedEventCarries()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			var raised = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
			EventHandler<MediaCapturedEventArgs> onCaptured = (_, e) => raised.TrySetResult(e.Media);
			host.View.MediaCaptured += onCaptured;

			var stream = await host.View.CaptureImage(CancellationToken.None).WaitAsync(CameraTimeout);

			Assert.Same(stream, await raised.Task.WaitAsync(CameraTimeout));
			GC.KeepAlive(onCaptured);
		});
	}

	[Fact]
	public async Task CaptureImageWithACancelledTokenThrowsOperationCanceledException()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();

			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.View.CaptureImage(cancellation.Token).WaitAsync(CameraTimeout));
		});
	}

	[Fact]
	public async Task CaptureImageStillWorksAfterACaptureWasCancelledBeforeItStarted()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			using var cancellation = new CancellationTokenSource();
			await cancellation.CancelAsync();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => host.View.CaptureImage(cancellation.Token).WaitAsync(CameraTimeout));

			Assert.True(IsJpeg(await CaptureJpegAsync(host.View)));
		});
	}

	[Fact]
	public async Task CaptureImageCalledTwiceAtTheSameTimeReturnsTwoSeparateJpegs()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			// The second capture waits for the first one (the semaphore), otherwise the first image would answer both calls.
			var first = host.View.CaptureImage(CancellationToken.None);
			var second = host.View.CaptureImage(CancellationToken.None);

			var streams = await Task.WhenAll(first, second).WaitAsync(CameraTimeout);

			Assert.NotSame(streams[0], streams[1]);
			Assert.True(IsJpeg(await ReadAllBytesAsync(streams[0])));
			Assert.True(IsJpeg(await ReadAllBytesAsync(streams[1])));
		});
	}

	[Fact]
	public async Task CaptureImageWhileTheCameraIsStoppedThrowsCameraExceptionWithTheNativeMessage()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StopCameraPreview();
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");

			var exception = await Assert.ThrowsAsync<CameraException>(() => host.View.CaptureImage(CancellationToken.None).WaitAsync(CameraTimeout));

			Assert.Contains("Not bound to a valid Camera", exception.Message);
		});
	}

	[Fact]
	public async Task CaptureImageWorksAgainAfterAFailedCapture()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StopCameraPreview();
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");
			await Assert.ThrowsAsync<CameraException>(() => host.View.CaptureImage(CancellationToken.None).WaitAsync(CameraTimeout));

			// The failure released the semaphore and unsubscribed the handlers.
			await host.View.StartCameraPreview(CancellationToken.None).WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
			Assert.True(IsJpeg(await CaptureJpegAsync(host.View)));
		});
	}

	[Fact]
	public async Task CaptureImageUnsubscribesItsHandlersWhenItIsDone()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			await CaptureJpegAsync(host.View);

			// A handler that was still subscribed would complete its finished TaskCompletionSource a second time and throw.
			var captured = Record.Exception(() => ((ICameraView)host.View).OnMediaCaptured(new MemoryStream()));
			var failed = Record.Exception(() => ((ICameraView)host.View).OnMediaCapturedFailed("Nobody waits for this."));

			Assert.Null(captured);
			Assert.Null(failed);
		});
	}

	// CameraX hands the result of a capture to the image callback of the manager, which raises the events of its own view: with the
	// callback of another view, the view under test never gets an image and CaptureImage ends when its token is cancelled. The preview
	// is stopped, so CameraX answers at once without taking a picture (which the camera HAL of the Android 9 emulator can not do).
	[Fact]
	public async Task CaptureImageThatNeverGetsAnImageHonorsItsToken()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StopCameraPreview();
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");
			var imageCallback = host.Native<object>("imageCallback");
			SetPrivateField(host.Manager, "imageCallback", CreateImageCallbackOf(new CameraView()));
			try
			{
				using var cancellation = new CancellationTokenSource();
				var capture = host.View.CaptureImage(cancellation.Token);
				// The timer starts once the capture waits for its image, so the token cannot expire before the capture began.
				cancellation.CancelAfter(TimeSpan.FromMilliseconds(500));

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(CameraTimeout));
			}
			finally
			{
				SetPrivateField(host.Manager, "imageCallback", imageCallback);
			}
		});
	}

	[Fact]
	public async Task CaptureImageWorksAgainAfterACaptureThatNeverGotAnImageWasCancelled()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			var imageCallback = host.Native<object>("imageCallback");
			SetPrivateField(host.Manager, "imageCallback", CreateImageCallbackOf(new CameraView()));
			try
			{
				using var cancellation = new CancellationTokenSource();
				var capture = host.View.CaptureImage(cancellation.Token);
				// The timer starts once the capture waits for its image, so the token cannot expire before the capture began.
				cancellation.CancelAfter(TimeSpan.FromMilliseconds(500));

				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => capture.WaitAsync(CameraTimeout));
			}
			finally
			{
				SetPrivateField(host.Manager, "imageCallback", imageCallback);
			}

			Assert.True(IsJpeg(await CaptureJpegAsync(host.View)));
		});
	}

	// The camera never started (the device has no camera, so the handler could not connect it): CaptureImage throws CameraException at
	// once, instead of waiting for a picture that can not come, which only its token could end.
	[Fact]
	public async Task CaptureImageBeforeTheCameraStartedThrowsCameraException()
	{
		SetupBuilder();
		UseCameraProvider(new StubCameraProvider { AvailableCameras = [] });

		await RunWithCameraViewAsync(async host =>
		{
			await SettleUiThreadAsync();
			Assert.Null(host.Native<ImageCapture>("imageCapture"));

			var exception = await Assert.ThrowsAsync<CameraException>(() => host.View.CaptureImage(CancellationToken.None).WaitAsync(CameraTimeout));

			Assert.Equal("Unable to capture an image because the camera is not initialized.", exception.Message);
		}, waitForStreaming: false);
	}

	// The capture that failed because the camera had not started (the manager has no ImageCapture before ConnectCamera created it)
	// released the capture semaphore and the event handlers.
	[Fact]
	public async Task CaptureImageWorksOnceTheCameraStartedAfterACaptureThatFailedBecauseItHadNotStarted()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			var imageCapture = host.Native<ImageCapture>("imageCapture");
			SetPrivateField(host.Manager, "imageCapture", null);
			try
			{
				await Assert.ThrowsAsync<CameraException>(() => host.View.CaptureImage(CancellationToken.None).WaitAsync(CameraTimeout));
			}
			finally
			{
				SetPrivateField(host.Manager, "imageCapture", imageCapture);
			}

			Assert.True(IsJpeg(await CaptureJpegAsync(host.View)));
		});
	}

	[Fact]
	public async Task StopAndStartCameraPreviewCommandsCloseAndOpenTheNativeCamera()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StopCameraPreviewCommand.Execute(null);
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");

			host.View.StartCameraPreviewCommand.Execute(CancellationToken.None);

			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "OPEN");
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);
		});
	}

	[Fact]
	public async Task CaptureImageCommandRaisesMediaCapturedWithAJpeg()
	{
		SetupBuilder();
		RequireStillCapture();

		await RunWithCameraViewAsync(async host =>
		{
			var raised = new TaskCompletionSource<Stream>(TaskCreationOptions.RunContinuationsAsynchronously);
			EventHandler<MediaCapturedEventArgs> onCaptured = (_, e) => raised.TrySetResult(e.Media);
			host.View.MediaCaptured += onCaptured;

			host.View.CaptureImageCommand.Execute(CancellationToken.None);

			Assert.True(IsJpeg(await ReadAllBytesAsync(await raised.Task.WaitAsync(CameraTimeout))));
			GC.KeepAlive(onCaptured);
		});
	}

	[Fact]
	public async Task VideoCommandsRecordAPlayableVideoIntoTheStreamOfTheStartCommand()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StartVideoRecordingCommand.Execute(stream);
			await WaitForRecordingDataAsync(before);

			host.View.StopVideoRecordingCommand.Execute(CancellationToken.None);

			// The command copies the recording into the stream and then rewinds it.
			await WaitUntilAsync(() => stream.Length > 0 && stream.Position == 0, "The stop command never delivered the video.");
			AssertIsAPlayableVideo(stream.ToArray());
		}, includeRecordAudio: true);
	}

	// The user taps stop and the page closes right away (both in one UI-thread message): the handler disposes the manager while the stop
	// of the async void StopVideoRecordingCommand still waits for CameraX to finalize the recording.
	[Fact]
	public async Task StopVideoRecordingCommandRightBeforeTheHandlerDisconnectsLetsNothingEscape()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			var disconnected = false;
			try
			{
				await WaitUntilTheHandlerConnectedTheCameraAsync(host.Handler);
				await host.View.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);
				await WaitForRecordingDataAsync(before);
				var finalized = host.Native<TaskCompletionSource>("videoRecordingFinalizeTcs")!;

				host.View.StopVideoRecordingCommand.Execute(CancellationToken.None);
				((IElementHandler)host.Handler).DisconnectHandler();
				disconnected = true;

				// The stop of the command goes on once CameraX finalized the recording (Dispose unbound its VideoCapture).
				await finalized.Task.WaitAsync(CameraTimeout);
				await SettleUiThreadAsync();
				Assert.Empty(UnhandledAsyncVoidExceptions);
			}
			finally
			{
				if (!disconnected)
				{
					((IElementHandler)host.Handler).DisconnectHandler();
				}
			}
		}, includeRecordAudio: true, disconnectWhenDone: false);
	}

	// ICommand.Execute cannot return the task of the work that a command starts: an exception that escaped a command would reach no caller and
	// end the app (the async void guard of the test base receives it here), and an app that binds the command in XAML cannot catch it. A command
	// reports its failure once instead, through the event that describes it.

	// The platform reports a capture that cannot start (no ImageCapture before the camera connected) through MediaCaptureFailed itself: the
	// command must not report it a second time, and the CameraException of CaptureImage must not escape.
	[Fact]
	public async Task CaptureImageCommandBeforeTheCameraStartedRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		SetupBuilder();
		UseCameraProvider(new StubCameraProvider { AvailableCameras = [] });

		await RunWithCameraViewAsync(async host =>
		{
			await SettleUiThreadAsync();
			var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
			EventHandler<MediaCaptureFailedEventArgs> onFailed = (_, e) => failures.Enqueue(e.FailureReason);
			host.View.MediaCaptureFailed += onFailed;

			host.View.CaptureImageCommand.Execute(CancellationToken.None);
			await SettleUiThreadAsync();

			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.Equal(["Unable to capture an image because the camera is not initialized."], failures);
			GC.KeepAlive(onFailed);
		}, waitForStreaming: false);
	}

	// StartCameraPreview reports a preview that cannot start (here a camera that CameraX cannot bind) through ErrorOccurred itself: the command
	// must not report it a second time, and before, the IllegalArgumentException of CameraX escaped the command.
	[Fact]
	public async Task StartCameraPreviewCommandOfACameraThatCanNotBeBoundRaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.StopCameraPreview();
			await WaitForNativeCameraStateAsync(host.SelectedCameraId, "CLOSED");

			// The stopped preview does not follow the selection: only the command starts the camera that cannot be bound.
			host.View.SelectedCamera = CreateCameraThatCanNotBeBound();
			await SettleUiThreadAsync();
			using var errors = new CameraErrorRecorder(host.View);

			host.View.StartCameraPreviewCommand.Execute(CancellationToken.None);
			await AssertionExtensions.AssertEventually(() => errors.Errors.Count > 0, 30000, 50, "The start of the camera that cannot be bound was not reported.");
			await SettleUiThreadAsync();

			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.IsType<Java.Lang.IllegalArgumentException>(Assert.Single(errors.Errors), exactMatch: false);
		});
	}

	// StopCameraPreviewCommand is a synchronous Command: an exception would escape to the caller of ICommand.Execute. CameraX unbinds the use
	// cases only on the main thread, so the command fails when it is executed on another thread.
	[Fact]
	public async Task StopCameraPreviewCommandOffTheUiThreadRaisesErrorOccurredOnceAndLetsNothingEscape()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			using var errors = new CameraErrorRecorder(host.View);
			var command = host.View.StopCameraPreviewCommand;

			var escaped = await Task.Run(() => Record.Exception(() => command.Execute(null)));

			Assert.Null(escaped);
			var exception = Assert.IsType<Java.Lang.IllegalStateException>(Assert.Single(errors.Errors), exactMatch: false);
			Assert.Contains("main thread", exception.Message);
		});
	}

	// The recording is stopped right after it started, before it recorded anything: CameraX finalizes it with ERROR_NO_VALID_DATA, and the
	// CameraException of StopVideoRecording used to escape the command.
	[Fact]
	public async Task StopVideoRecordingCommandOfARecordingThatRecordedNothingRaisesMediaCaptureFailedOnceAndLetsNothingEscape()
	{
		SetupBuilder();
		RequireVideoRecording();

		await RunWithCameraViewAsync(async host =>
		{
			var failures = new System.Collections.Concurrent.ConcurrentQueue<string>();
			var failed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			EventHandler<MediaCaptureFailedEventArgs> onFailed = (_, e) =>
			{
				failures.Enqueue(e.FailureReason);
				failed.TrySetResult();
			};
			host.View.MediaCaptureFailed += onFailed;
			await host.View.StartVideoRecording(new MemoryStream(), CancellationToken.None).WaitAsync(CameraTimeout);

			host.View.StopVideoRecordingCommand.Execute(CancellationToken.None);

			await failed.Task.WaitAsync(CameraTimeout);
			await SettleUiThreadAsync();
			Assert.Empty(UnhandledAsyncVoidExceptions);
			Assert.Contains($"VideoRecordEvent.Finalize error code {AndroidX.Camera.Video.VideoRecordEvent.Finalize.ErrorNoValidData}", Assert.Single(failures));
			GC.KeepAlive(onFailed);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StartVideoRecordingWithoutAStreamRecordsIntoANewMemoryStream()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			await host.View.StartVideoRecording().WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			var stream = await host.View.StopVideoRecording().WaitAsync(CameraTimeout);

			var memory = Assert.IsType<MemoryStream>(stream);
			Assert.Equal(0, memory.Position);
			AssertIsAPlayableVideo(memory.ToArray());
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingOfTheStreamTypeReturnsTheStreamTheRecordingWasStartedWith()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();
		var stream = new MemoryStream();

		await RunWithCameraViewAsync(async host =>
		{
			await host.View.StartVideoRecording(stream).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			var stopped = await host.View.StopVideoRecording<MemoryStream>().WaitAsync(CameraTimeout);

			Assert.Same(stream, stopped);
			AssertIsAPlayableVideo(stream.ToArray());
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingOfAnotherStreamTypeThrowsInvalidCastException()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			await host.View.StartVideoRecording(new MemoryStream()).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			await Assert.ThrowsAsync<InvalidCastException>(() => host.View.StopVideoRecording<FileStream>());
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task StopVideoRecordingWithoutARecordingReturnsStreamNull()
	{
		SetupBuilder();

		await RunWithCameraViewAsync(async host =>
		{
			var stopped = await host.View.StopVideoRecording().WaitAsync(CameraTimeout);

			Assert.Same(Stream.Null, stopped);
		});
	}

	// StartVideoRecording and StopVideoRecording build new use cases and bind the photo or video session again, and CameraX starts a
	// bound camera with its defaults.
	[Fact]
	public async Task FlashModeOfTheViewIsStillAppliedAfterARecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.CameraFlashMode = CameraFlashMode.On;
			await host.View.StartVideoRecording(new MemoryStream()).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			await host.View.StopVideoRecording().WaitAsync(CameraTimeout);

			Assert.Equal(CameraFlashMode.On, host.View.CameraFlashMode);
			Assert.Equal(ImageCapture.FlashModeOn, host.Native<ImageCapture>("imageCapture")!.FlashMode);
		}, includeRecordAudio: true);
	}

	[Fact]
	public async Task ZoomFactorOfTheViewStillDescribesTheNativeZoomAfterARecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.MaximumZoomFactor > 1f);
		Skip.If(camera is null, "No camera of this device can zoom in.");
		var ratio = Math.Min(2f, camera.MaximumZoomFactor);
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.ZoomFactor = ratio;
			await WaitForNativeZoomAsync(camera.DeviceId, ratio);
			await host.View.StartVideoRecording(new MemoryStream()).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			await host.View.StopVideoRecording().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);

			// Either the zoom is kept or the view is reset: the two must agree.
			await WaitForNativeZoomAsync(camera.DeviceId, host.View.ZoomFactor,
				message: $"After the recording the native zoom ratio of camera {camera.DeviceId} is not CameraView.ZoomFactor ({host.View.ZoomFactor}).");
		}, new CameraView { SelectedCamera = camera }, includeRecordAudio: true);
	}

	[Fact]
	public async Task TorchOfTheViewStillDescribesTheNativeTorchAfterARecording()
	{
		SetupBuilder();
		RequireVideoRecording();
		var camera = (await GetRealCamerasAsync()).FirstOrDefault(camera => camera.IsFlashSupported);
		Skip.If(camera is null, "No camera of this device has a flash unit, so the torch can not be switched on.");
		var before = ListRecordingFiles();

		await RunWithCameraViewAsync(async host =>
		{
			host.View.IsTorchOn = true;
			await WaitForNativeTorchAsync(camera.DeviceId, true);
			await host.View.StartVideoRecording(new MemoryStream()).WaitAsync(CameraTimeout);
			await WaitForRecordingDataAsync(before);

			await host.View.StopVideoRecording().WaitAsync(CameraTimeout);
			await WaitUntilCameraIsStreamingAsync(host.View, host.PreviewView);

			// Either the torch is kept or the view is reset: the two must agree.
			await WaitForNativeTorchAsync(camera.DeviceId, host.View.IsTorchOn,
				message: $"After the recording the native torch of camera {camera.DeviceId} does not match CameraView.IsTorchOn ({host.View.IsTorchOn}).");
		}, new CameraView { SelectedCamera = camera }, includeRecordAudio: true);
	}
}