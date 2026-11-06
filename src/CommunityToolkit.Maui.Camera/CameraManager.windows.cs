using System.Diagnostics;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Maui.Extensions;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	MediaPlayerElement? mediaElement;
	MediaCapture? mediaCapture;
	MediaFrameSource? frameSource;
	LowLagMediaRecording? mediaRecording;
	Stream? videoCaptureStream;

	// The video of a stop that its token cancelled after the recording was finished: the next StopVideoRecording returns it, as on Android, iOS
	// and Mac Catalyst, where the token only cancels waiting for the video
	Stream? finishedVideoStream;

	// A recording that turned on the camera of a preview that the app had stopped turns it off again when the recording ends, as on Android, iOS
	// and Mac Catalyst. A preview that the app started (also one whose camera could not be opened) stays started.
	bool isCameraStartedForTheRecording;

	// The restart of a camera switch keeps a camera that only a recording turned on, and the StopVideoRecording that then finds no recording turns
	// it off, as on iOS and Mac Catalyst. Without it a camera switch during such a recording left the camera on for good.
	bool isCameraSwitchOfARecordingCamera;

	// The length and the position that the stream of the recording had when the recording was prepared: a recording that a later start replaces
	// is discarded, so its stream goes back to them, as the stream of a discarded recording stays untouched on Android, iOS and Mac Catalyst
	long videoCaptureStreamLength;
	long videoCaptureStreamPosition;

	// A second start joins the first, and a stop waits for it, as on Android, iOS and Mac Catalyst. Without it a stop during the camera-on left the
	// camera recording, and a double tap failed the first start by replacing its MediaCapture.
	Task pendingVideoRecordingStart = Task.CompletedTask;

	// A stop waits for an earlier stop, and a start for a stop, as on Android, iOS and Mac Catalyst: the stop of a recording that turned the camera
	// on turns the camera off only once Media Foundation has finished the recording. Without it a second stop (a double tap) turned the camera off
	// under the first stop, which lost its video, and a start right after a stop prepared its recording on the camera that the stop then released.
	Task pendingVideoRecordingStop = Task.CompletedTask;

	// The start of the preview and the start of a recording wait for each other instead of replacing each other's MediaCapture: a recording
	// that replaced the app's pending preview would turn off a preview that the app started
	Task pendingPreviewStart = Task.CompletedTask;

	// The device tests replace them: their machines have no camera, so they script when and how Media Foundation answers a start, and the frame
	// source of a MediaCapture that it never initialized
	Func<MediaCapture, string, CancellationToken, Task<bool>> initializeMediaCapture =
		static (capture, deviceId, token) => capture.InitializeCameraForCameraView(deviceId, token);

	Func<MediaCapture, MediaFrameSource?> findColorVideoFrameSource =
		static capture => capture.FrameSources
			.FirstOrDefault(static source => source.Value.Info.MediaStreamType is MediaStreamType.VideoRecord && source.Value.Info.SourceKind is MediaFrameSourceKind.Color)
			.Value;

	Func<MediaCapture, ImageEncodingProperties, CancellationToken, Task> setPhotoStreamProperties =
		static (capture, properties, token) => capture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, properties).AsTask(token);

	Func<MediaCapture, MediaEncodingProfile, IRandomAccessStream, CancellationToken, Task<LowLagMediaRecording>> prepareLowLagRecording =
		static (capture, profile, stream, token) => capture.PrepareLowLagRecordToStreamAsync(profile, stream).AsTask(token);

	public MediaPlayerElement CreatePlatformView()
	{
		mediaElement = new MediaPlayerElement();
		return mediaElement;
	}

	public void Dispose()
	{
		finishedVideoStream = null;
		PlatformStopCameraPreview();
		mediaCapture?.Dispose();
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		if (!isInitialized || (mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
		{
			return;
		}

		if (mediaCapture is null)
		{
			return;
		}

		var (updatedFlashControlEnabled, updatedFlashControlAuto) = flashMode switch
		{
			CameraFlashMode.Off => (false, (bool?)null),
			CameraFlashMode.On => (true, false),
			CameraFlashMode.Auto => (true, true),
			_ => throw new NotSupportedException($"{flashMode} is not yet supported")
		};

		mediaCapture.VideoDeviceController.FlashControl.Enabled = updatedFlashControlEnabled;

		if (updatedFlashControlAuto.HasValue)
		{
			mediaCapture.VideoDeviceController.FlashControl.Auto = updatedFlashControlAuto.Value;
		}

	}

	public partial void UpdateIsTorchOn(bool isTorchOn)
	{
		if (!isInitialized ||
			mediaCapture is null ||
			!(mediaCapture.VideoDeviceController?.TorchControl?.Supported ?? false))
		{
			return;
		}

		bool isCurrentlyOn = mediaCapture.VideoDeviceController.TorchControl.Enabled;

		if (isTorchOn != isCurrentlyOn)
		{
			mediaCapture.VideoDeviceController.TorchControl.Enabled = isTorchOn;
		}
	}

	public partial void UpdateZoom(float zoomLevel)
	{
		if (!isInitialized || mediaCapture is null || !mediaCapture.VideoDeviceController.ZoomControl.Supported)
		{
			return;
		}

		var step = mediaCapture.VideoDeviceController.ZoomControl.Step;

		if (zoomLevel % step != 0)
		{
			zoomLevel = (float)Math.Ceiling(zoomLevel / step) * step;
		}

		mediaCapture.VideoDeviceController.ZoomControl.Value = zoomLevel;
	}

	public async partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		await PlatformUpdateResolution(resolution, token);
	}

	static async Task ReleaseTheCancelledRecordingAsync(LowLagMediaRecording recording, bool isRecordingStarted)
	{
		try
		{
			if (isRecordingStarted)
			{
				await recording.StopAsync();
			}

			await recording.FinishAsync();
		}
		catch (Exception exception)
		{
			Trace.TraceWarning($"{nameof(CameraManager)}: unable to release the cancelled video recording: {exception}");
		}
	}

	private partial void PlatformDisconnect()
	{
	}

	private async partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		if (mediaCapture is null)
		{
			// CameraView.CaptureImage waits for MediaCaptured or MediaCaptureFailed, so a capture that cannot start must be reported
			cameraView.OnMediaCapturedFailed("Unable to capture an image because the camera is not initialized. Start the camera preview first.");
			return;
		}

		token.ThrowIfCancellationRequested();

		MemoryStream memoryStream = new();

		try
		{
			await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());

			memoryStream.Position = 0;

			cameraView.OnMediaCaptured(memoryStream);
		}
		catch (Exception ex)
		{
			// Reported like on Android, iOS and Mac Catalyst: CameraView.CaptureImage then throws the CameraException of MediaCaptureFailed
			// (a rethrow here made it throw the platform exception instead and left that CameraException unobserved)
			cameraView.OnMediaCapturedFailed(ex.Message);
		}
	}

	private partial Task PlatformConnectCamera(CancellationToken token) => PlatformStartCameraPreview(token);

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		await WaitForTheEarlierOperationAsync(pendingVideoRecordingStart, token);

		// A preview that the app starts is the app's to stop, also when a recording had turned the camera on before. The flag is set after the wait,
		// because the recording that was waited for sets it. Only the restart of a camera switch keeps the camera of a recording.
		isCameraStartedForTheRecording = isCameraSwitchOfARecordingCamera;
		isCameraSwitchOfARecordingCamera = false;

		var start = StartPreviewAsync(cameraView.OnErrorOccurred, token);
		pendingPreviewStart = start;
		await start;
	}

	static async Task WaitForTheEarlierOperationAsync(Task earlierOperation, CancellationToken token)
	{
		if (earlierOperation.IsCompleted)
		{
			// Nothing to wait for: the operation behaves as it did before the wait existed, also with a token that is cancelled already (a
			// StopVideoRecording with a cancelled token still stops the recording)
			return;
		}

		try
		{
			await earlierOperation.WaitAsync(token);
		}
		catch (Exception) when (!token.IsCancellationRequested)
		{
			// The earlier operation reports its own failure to its caller
		}

		token.ThrowIfCancellationRequested();
	}

	// The failures of the start go to reportFailure: the preview reports them through ErrorOccurred, while a recording that turns the camera on
	// throws the failure to its caller, as on iOS and Mac Catalyst, so that the failure is reported once
	async Task StartPreviewAsync(Action<Exception> reportFailure, CancellationToken token)
	{
		if (mediaElement is null)
		{
			return;
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault();

		if (cameraView.SelectedCamera is null)
		{
			reportFailure(new CameraException("Couldn't start camera preview; no cameras available"));
			return;
		}

		// A start that StopCameraPreview cancelled before it began (on the connection of the view) must not open a camera
		token.ThrowIfCancellationRequested();

		// A preview that runs is not started again: a second MediaCapture would replace the one that shows the preview and was never released
		if (isInitialized && mediaCapture is not null)
		{
			return;
		}

		// The MediaCapture of a start that still waits for Media Foundation is released now, not when Media Foundation answers (which a camera
		// that hangs never does). That start ends without reporting anything, as it does when a StopCameraPreview releases its MediaCapture.
		mediaCapture?.Dispose();

		// In the field while Media Foundation initializes it, so that StopCameraPreview and Dispose release it at once. A newer start can replace and
		// release it there meanwhile, so this start releases only its own MediaCapture
		var startedMediaCapture = new MediaCapture();
		mediaCapture = startedMediaCapture;

		try
		{
			if (!await initializeMediaCapture(startedMediaCapture, cameraView.SelectedCamera.DeviceId, token))
			{
				throw new CameraException("Unable to open the selected camera. It may be unavailable or in use by another application.");
			}

			// StopCameraPreview cancels a start and releases the MediaCapture that the start initializes: an initialization that Media Foundation
			// completes all the same must not go on with it
			token.ThrowIfCancellationRequested();
		}
		catch (Exception ex)
		{
			// A MediaCapture that Media Foundation could not initialize cannot preview or capture, whatever stopped its initialization (a camera
			// that cannot be opened, the cancellation of the token, a camera that the privacy settings block)
			var ownsThePreview = OwnsThePreview();
			if (ownsThePreview)
			{
				mediaCapture = null;
			}

			startedMediaCapture.Dispose();

			if (ex is OperationCanceledException)
			{
				throw;
			}

			// Media Foundation can also fail to initialize the MediaCapture that StopCameraPreview released: a start that was cancelled reports no
			// error
			token.ThrowIfCancellationRequested();

			// The start that replaced this start's MediaCapture (a newer start, or a camera switch) decides about the preview, and a released view
			// reports nothing
			if (!ownsThePreview)
			{
				return;
			}

			// A camera that cannot be opened (it does not exist any more, or another app uses it) leaves the preview started, so that selecting
			// another camera starts the preview with that camera (UpdateCurrentCamera). A camera that the privacy settings block
			// (UnauthorizedAccessException) leaves it stopped until StartCameraPreview is called again
			isInitialized = ex is not UnauthorizedAccessException;

			reportFailure(ex);

			return;
		}

		// A newer start, a camera switch or the release of the view can replace this start's MediaCapture while the start waits for Media Foundation
		if (!OwnsThePreview())
		{
			startedMediaCapture.Dispose();
			return;
		}

		frameSource = findColorVideoFrameSource(startedMediaCapture);

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		isInitialized = true;

		try
		{
			await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

			// Media Foundation can complete the resolution before a StopCameraPreview that ran before this continuation: that stop released the
			// preview
			token.ThrowIfCancellationRequested();
		}
		catch (Exception ex) when (token.IsCancellationRequested || !OwnsThePreview())
		{
			// As during the initialization, a start that was cancelled, or whose MediaCapture was released or replaced meanwhile, reports nothing,
			// also when Media Foundation fails the resolution of the released MediaCapture. Unlike then, its preview shows already: a start that its
			// own token cancelled stops it again
			if (OwnsThePreview())
			{
				PlatformStopCameraPreview();
			}
			else
			{
				startedMediaCapture.Dispose();
			}

			if (ex is OperationCanceledException)
			{
				throw;
			}

			token.ThrowIfCancellationRequested();

			return;
		}

		if (!OwnsThePreview())
		{
			startedMediaCapture.Dispose();
			return;
		}

		onLoaded.Invoke();

		bool OwnsThePreview() => ReferenceEquals(mediaCapture, startedMediaCapture);
	}

	private partial void PlatformStopCameraPreviewForCameraSwitch()
	{
		var isStartedForTheRecording = isCameraStartedForTheRecording;

		PlatformStopCameraPreview();

		isCameraSwitchOfARecordingCamera = isStartedForTheRecording;
	}

	private partial void PlatformStopCameraPreview()
	{
		if (mediaElement is null)
		{
			return;
		}

		mediaElement.Source = null;
		mediaCapture?.Dispose();

		// Releasing the MediaCapture ends the recording that it runs, so the recording is forgotten with its stream: a later StopVideoRecording
		// has nothing to stop, and a preview that starts again does not find a recording of the released MediaCapture
		mediaRecording = null;
		videoCaptureStream = null;
		frameSource = null;

		mediaCapture = null;
		isInitialized = false;
		isCameraStartedForTheRecording = false;
		isCameraSwitchOfARecordingCamera = false;
	}

	async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (cameraView.SelectedCamera is null || !isInitialized || mediaCapture is null)
		{
			return;
		}

		var filteredPropertiesList = cameraView.SelectedCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height).ToList();

		if (filteredPropertiesList.Count is 0)
		{
			filteredPropertiesList = [.. cameraView.SelectedCamera.ImageEncodingProperties.OrderByDescending(p => p.Width * p.Height)];
		}

		if (filteredPropertiesList.Count is not 0)
		{
			await setPhotoStreamProperties(mediaCapture, filteredPropertiesList.First(), token);
		}
	}

	private partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		if (!pendingVideoRecordingStart.IsCompleted)
		{
			return pendingVideoRecordingStart.WaitAsync(token);
		}

		var start = StartVideoRecordingAsync(stream, token);
		pendingVideoRecordingStart = start;

		return start;
	}

	async Task StartVideoRecordingAsync(Stream stream, CancellationToken token)
	{
		if (mediaElement is null)
		{
			return;
		}

		await WaitForTheEarlierOperationAsync(pendingVideoRecordingStop, token);
		await WaitForTheEarlierOperationAsync(pendingPreviewStart, token);

		// A start whose token is cancelled already throws OperationCanceledException before it turns the camera on, as on Android, iOS and Mac Catalyst,
		// also without a camera, whose failure the start of the camera would report first
		token.ThrowIfCancellationRequested();

		// Like on Android, iOS and Mac Catalyst, recording turns the camera on. A preview that is marked as started but has no MediaCapture (its
		// camera could not be opened) is started again as well, and stays marked as started when that fails
		var isCameraTurnedOnHere = await DiscardTheRunningRecordingAsync(stream);
		if (!isInitialized || mediaCapture is null)
		{
			await TurnTheCameraOnForTheRecordingAsync(token);
			isCameraTurnedOnHere = isCameraStartedForTheRecording;
		}

		try
		{
			await RecordAsync(stream, token);
		}
		catch
		{
			// The camera that this start turned on is off again when its recording cannot start. A recording that runs already keeps its camera
			if (isCameraTurnedOnHere && isCameraStartedForTheRecording)
			{
				PlatformStopCameraPreview();
			}

			throw;
		}
	}

	// Like on Android, iOS and Mac Catalyst, the stream of the latest start receives the video and the recording that ran before is discarded.
	// Returns whether the camera belongs to the new recording because the discarded one had turned it on: it is off again when the new recording
	// cannot start, as when the new recording turns the camera on itself.
	async Task<bool> DiscardTheRunningRecordingAsync(Stream newStream)
	{
		// A start replaces the video that a cancelled stop left for the next stop, as on Android, iOS and Mac Catalyst
		if (finishedVideoStream is Stream finishedStream)
		{
			finishedVideoStream = null;
			RestoreTheStreamOfTheDiscardedRecording(finishedStream);
		}

		if (mediaRecording is null || videoCaptureStream is null)
		{
			return false;
		}

		// A stream that is refused (AsRandomAccessStream throws for a stream that cannot seek) must not end the recording that runs
		_ = newStream.AsRandomAccessStream();

		// A recording that is not finished leaves an MP4 without its moov box, and Media Foundation writes into the stream while it records.
		// The camera stays on: the new recording prepares on the same MediaCapture.
		var recording = mediaRecording;
		var stream = videoCaptureStream;
		mediaRecording = null;
		videoCaptureStream = null;

		try
		{
			await recording.StopAsync();
			await recording.FinishAsync();
		}
		finally
		{
			RestoreTheStreamOfTheDiscardedRecording(stream);
		}

		return isCameraStartedForTheRecording;
	}

	void RestoreTheStreamOfTheDiscardedRecording(Stream stream)
	{
		// A stream that the app disposed or made read-only meanwhile keeps what it holds: the discarded video is not worth failing the new start
		if (stream.CanSeek && stream.CanWrite)
		{
			stream.SetLength(videoCaptureStreamLength);
		}

		if (stream.CanSeek)
		{
			stream.Position = videoCaptureStreamPosition;
		}
	}

	async Task TurnTheCameraOnForTheRecordingAsync(CancellationToken token)
	{
		var isPreviewStopped = !isInitialized;
		Exception? failure = null;

		try
		{
			await StartPreviewAsync(exception => failure ??= exception, token);
		}
		catch (Exception exception) when (exception is not OperationCanceledException && isPreviewStopped)
		{
			// Media Foundation opened the camera and failed what came after it (the capture resolution): the preview that the app stopped stays
			// stopped, as on iOS and Mac Catalyst
			PlatformStopCameraPreview();
			throw;
		}

		if (failure is not null)
		{
			// The preview that the app stopped stays stopped: the failed start marks a preview whose camera cannot be opened as started
			if (isPreviewStopped)
			{
				isInitialized = false;
			}

			ExceptionDispatchInfo.Capture(failure).Throw();
		}

		// A newer start replaced the MediaCapture of this start, which is not initialized yet: the recording has no camera to record with
		if (!isInitialized || mediaCapture is null)
		{
			throw new CameraException("Unable to start video recording because the camera could not be started.");
		}

		isCameraStartedForTheRecording = isPreviewStopped;
	}

	async Task RecordAsync(Stream stream, CancellationToken token)
	{
		if (!isInitialized || mediaCapture is null || mediaElement is null)
		{
			return;
		}

		var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
		var recordingMediaCapture = mediaCapture;
		var randomAccessStream = stream.AsRandomAccessStream();
		var streamLength = stream.Length;
		var streamPosition = stream.Position;
		var preparedRecording = await prepareLowLagRecording(mediaCapture, profile, randomAccessStream, token);

		// Media Foundation can complete the preparation before a StopCameraPreview that ran before this continuation and released the MediaCapture
		// of the prepared recording: StopVideoRecording must not find the recording of a start that the stop cancelled
		if (token.IsCancellationRequested)
		{
			// A preparation that only the caller's token cancelled belongs to the MediaCapture that still shows the preview: it is released like a
			// start that its token cancelled later
			if (ReferenceEquals(mediaCapture, recordingMediaCapture))
			{
				await ReleaseTheCancelledRecordingAsync(preparedRecording, isRecordingStarted: false);
			}

			token.ThrowIfCancellationRequested();
		}

		mediaRecording = preparedRecording;

		// Only a recording that could be prepared replaces the stream: a stream that is refused (AsRandomAccessStream throws for a stream
		// that cannot seek) must not replace the stream of a recording that is running
		videoCaptureStream = stream;
		videoCaptureStreamLength = streamLength;
		videoCaptureStreamPosition = streamPosition;

		var isRecordingStarted = false;

		try
		{
			frameSource = findColorVideoFrameSource(mediaCapture);

			if (frameSource is not null)
			{
				var frameFormat = frameSource.SupportedFormats
						.OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height)
						.FirstOrDefault();

				if (frameFormat is not null)
				{
					await frameSource.SetFormatAsync(frameFormat).AsTask(token);

					// The source that a StopCameraPreview cleared meanwhile stays cleared
					token.ThrowIfCancellationRequested();

					mediaElement.AutoPlay = true;
					mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
					await preparedRecording.StartAsync();
					isRecordingStarted = true;

					// A StopCameraPreview during the start released the MediaCapture and with it the recording, which the start did not wait for
					token.ThrowIfCancellationRequested();
				}
			}
		}
		catch (OperationCanceledException) when (ReferenceEquals(mediaRecording, preparedRecording))
		{
			// The caller's token cancelled the start (a StopCameraPreview forgets the recording itself): StopVideoRecording must not find a
			// recording that its start reported as cancelled
			mediaRecording = null;
			videoCaptureStream = null;

			await ReleaseTheCancelledRecordingAsync(preparedRecording, isRecordingStarted);

			throw;
		}
	}

	private partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		// Read before the first await: a start or a stop that arrives later waits for this stop, so this stop must not wait for it
		var earlierStop = pendingVideoRecordingStop;
		var stop = StopVideoRecordingAsync(earlierStop, pendingVideoRecordingStart, token);

		// A stop that its token cancels while it waits for the earlier stop ends before that stop: the start or stop that comes next still waits for the
		// earlier stop, as the semaphore of Android, iOS and Mac Catalyst makes it wait. Without it that call turned the camera off under the earlier
		// stop, which lost its video, or prepared its recording on the camera that the earlier stop then released.
		pendingVideoRecordingStop = WhenBothEndAsync(earlierStop, stop);

		return stop;
	}

	// Never faults: the failure of a stop goes to the caller of that stop, not to the start or stop that waits for it
	static async Task WhenBothEndAsync(Task earlierStop, Task stop)
	{
		await WaitForTheEarlierOperationAsync(earlierStop, CancellationToken.None);
		await WaitForTheEarlierOperationAsync(stop, CancellationToken.None);
	}

	async Task<Stream> StopVideoRecordingAsync(Task earlierStop, Task earlierStart, CancellationToken token)
	{
		await WaitForTheEarlierOperationAsync(earlierStop, token);
		await WaitForTheEarlierOperationAsync(earlierStart, token);

		if (finishedVideoStream is Stream finishedStream)
		{
			// A stop that its token cancelled finished the recording already: the token only cancels waiting for the video
			token.ThrowIfCancellationRequested();
			finishedVideoStream = null;

			return finishedStream;
		}

		if (!isInitialized || mediaElement is null || mediaRecording is null || videoCaptureStream is null)
		{
			// No recording is left to stop (a camera switch, or a start that was cancelled, released it): the camera that only a recording turned
			// on is off again all the same
			if (isCameraStartedForTheRecording)
			{
				PlatformStopCameraPreview();
			}

			return Stream.Null;
		}

		// The stop ends the recording, as on Android, iOS and Mac Catalyst: a later StopVideoRecording finds no recording and returns
		// Stream.Null instead of stopping the stopped LowLagMediaRecording again, which throws a COMException
		var recording = mediaRecording;
		var stream = videoCaptureStream;
		mediaRecording = null;
		videoCaptureStream = null;

		try
		{
			await recording.StopAsync();

			// Releases the resources of the recording; LowLagMediaRecording cannot be started again after FinishAsync
			await recording.FinishAsync();
		}
		finally
		{
			// The camera that the recording turned on is off again, also when the recording cannot be stopped
			if (isCameraStartedForTheRecording)
			{
				PlatformStopCameraPreview();
			}
		}

		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		if (token.IsCancellationRequested)
		{
			// The recording is finished, so the next StopVideoRecording returns its video, as on Android, iOS and Mac Catalyst
			finishedVideoStream = stream;
			token.ThrowIfCancellationRequested();
		}

		return stream;
	}
}