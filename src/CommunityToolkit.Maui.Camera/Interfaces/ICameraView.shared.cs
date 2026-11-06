namespace CommunityToolkit.Maui.Core;

/// <summary>
/// Represents a visual element that provides the ability to show a camera preview and capture images.
/// </summary>
public interface ICameraView : IView
{
	/// <summary>
	/// Gets the <see cref="CameraFlashMode"/>.
	/// </summary>
	CameraFlashMode CameraFlashMode { get; }

	/// <summary>
	/// Gets or sets the resolution at which the camera will capture images.
	/// </summary>
	/// <remarks>
	/// On iOS and Mac Catalyst the camera uses its largest format that fits within this resolution. When no format fits, for example with the
	/// default <see cref="Size.Zero"/>, the camera keeps its current format. Until a resolution that fits one of its formats has been applied,
	/// that is the format that the photo session preset (AVCaptureSessionPresetPhoto) chose. Once a format has been applied, the camera keeps
	/// the last applied format, and a camera selected later keeps its own current format.
	/// </remarks>
	Size ImageCaptureResolution { get; }

	/// <summary>
	/// Gets a value indicating whether the torch (flash) is on.
	/// </summary>
	bool IsTorchOn { get; }

	/// <summary>
	/// Gets or sets the currently selected camera.
	/// </summary>
	/// <remarks>
	/// This property will be <c>null</c> if no camera is selected.
	/// Changing it while the camera preview runs restarts the preview with the new camera, and a <see cref="StopCameraPreview"/> before the new
	/// camera has started keeps the preview stopped. When the new camera cannot be started (for example because another app uses it), the
	/// preview stays stopped, the view reports the failure through its ErrorOccurred event, and selecting another camera starts the preview
	/// with that camera. The same holds after a start of the preview (when the view connects its camera, or
	/// <see cref="StartCameraPreview(CancellationToken)"/>) that failed because of the selected camera, until <see cref="StopCameraPreview"/>
	/// is called: after it, also when it interrupted such a start, changing this property does not start the preview. After a start that failed
	/// because the camera permission is not granted, changing it does not start the preview: call
	/// <see cref="StartCameraPreview(CancellationToken)"/> once the app has the permission (on Android the preview also starts when the activity
	/// resumes with the permission, see <see cref="StartCameraPreview(CancellationToken)"/>).
	/// On Android, selecting another camera while a video recording runs does not end the recording, nor does selecting the camera of the
	/// recording again (for example after <c>null</c>): the change waits until the recording is stopped or replaced. <see cref="StopVideoRecording(CancellationToken)"/> applies the change before it returns, and
	/// <see cref="StartVideoRecording(CancellationToken)"/> or <see cref="StartVideoRecording(Stream, CancellationToken)"/> applies it before it
	/// starts the new recording, which then uses the selected camera. Until then this property returns the selected camera, while the
	/// recording, the preview and <see cref="CaptureImage(CancellationToken)"/> keep using the camera of the recording.
	/// <see cref="StopCameraPreview"/> still stops the preview and the recording, and the next <see cref="StartCameraPreview(CancellationToken)"/>
	/// starts the selected camera.
	/// </remarks>
	CameraInfo? SelectedCamera { get; internal set; }

	/// <summary>
	/// Gets or sets the current zoom factor of the camera.
	/// </summary>
	/// <remarks>
	/// If a camera is selected and the zoom factor is set to a value outside the range of the camera's supported zoom factors,
	/// the value will be coerced to the nearest supported zoom factor.
	/// If no camera is selected, the value will be set as-is.
	/// </remarks>
	float ZoomFactor { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the camera feature is available on the current device.
	/// </summary>
	bool IsAvailable { get; internal set; }

	/// <summary>
	/// Gets a value indicating whether the camera is currently busy.
	/// </summary>
	bool IsBusy { get; internal set; }

	/// <summary>
	/// Triggers the camera to capture an image.
	/// </summary>
	/// <remarks>
	/// To customize the behavior of the camera when capturing an image, consider overriding the behavior through
	/// <c>CameraViewHandler.CommandMapper.ReplaceMapping(nameof(ICameraView.CaptureImage), ADD YOUR METHOD);</c>.
	/// The capture ends with the platform's answer to it. A failure that the view reports through its MediaCaptureFailed event while the capture
	/// waits for that answer, for example of a video recording that its command cannot start, does not end the capture.
	/// </remarks>
	/// <exception cref="CameraException">Thrown when the platform reports that the image could not be captured, for example when the camera
	/// preview has not been started; the view raises its MediaCaptureFailed event with the same reason.</exception>
	Task<Stream> CaptureImage(CancellationToken token);

	/// <summary>
	/// Starts the camera preview.
	/// </summary>
	/// <remarks>
	/// To customize the behavior of starting the camera preview, consider overriding the behavior through
	/// <c>CameraViewHandler.CommandMapper.ReplaceMapping(nameof(ICameraView.StartCameraPreview), ADD YOUR METHOD);</c>.
	/// A preview that cannot start is reported through the ErrorOccurred event of the view instead of an exception, on every platform (except
	/// the Android start that waited for the connection of the view, see below): for example no camera is available, the camera permission is
	/// not granted, or the selected camera cannot be opened (another app uses it, or it was unplugged). A preview whose selected camera cannot
	/// be opened counts as started: selecting another camera starts the preview with that camera (see <see cref="SelectedCamera"/>). A preview
	/// without the camera permission stays stopped until this method is called again once the app has the permission.
	/// On Android, a start that failed only because the app did not have the camera permission (it had not asked for it yet, or the user denied
	/// it) is retried when the activity of the view resumes with the permission granted, for example once the user allowed it in the permission
	/// dialog or in the settings of the app: a view that connects its camera while the app asks for the permission starts its preview without a
	/// call to this method. The retry reports its failure through the ErrorOccurred event. Only the last start counts: <see cref="StopCameraPreview"/>
	/// cancels the retry, and so does a later start, whether it succeeds, fails for another reason or is cancelled.
	/// <see cref="StopCameraPreview"/> cancels a start that has not completed yet, like a cancellation of <paramref name="token"/>: the preview stays
	/// stopped, the start throws <see cref="OperationCanceledException"/>, and the ErrorOccurred event is not raised for it, also when the stop
	/// made the start fail. The <see cref="OperationCanceledException.CancellationToken"/> of the exception is <paramref name="token"/> only when
	/// <paramref name="token"/> cancelled the start.
	/// On Android, a start while the view connects its camera waits for the connection, then starts the preview. When the connection failed and
	/// reported that failure through the ErrorOccurred event, the start that waited for it and fails as well throws the failure to its caller
	/// without raising the event a second time.
	/// On Android, a start that arrives while another start waits for CameraX waits for it too: when the other start started the preview with the
	/// selected camera, this start returns without starting it a second time. A start that fails because the view was released meanwhile ends
	/// without an exception and without the ErrorOccurred event.
	/// </remarks>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is cancelled, or <see cref="StopCameraPreview"/> is
	/// called, before the preview started.</exception>
	/// <exception cref="CameraException">Thrown on Android by a start that waited for the view to connect its camera and fails after the connection
	/// reported its own failure through the ErrorOccurred event (for example without the camera permission).</exception>
	/// <exception cref="InvalidOperationException">Thrown when the view has no handler, for example before it is shown or after its handler was
	/// disconnected.</exception>
	Task StartCameraPreview(CancellationToken token);

	/// <summary>
	/// Stops the camera preview.
	/// </summary>
	/// <remarks>
	/// To customize the behavior of stopping the camera preview, consider overriding the behavior through
	/// <c>CameraViewHandler.CommandMapper.ReplaceMapping(nameof(ICameraView.StopCameraPreview), ADD YOUR METHOD);</c>.
	/// The stop wins over the work that would start the preview or a recording and has not completed yet. It cancels a
	/// <see cref="StartCameraPreview(CancellationToken)"/>, <see cref="StartVideoRecording(CancellationToken)"/> or
	/// <see cref="StartVideoRecording(Stream, CancellationToken)"/> that is still in progress, which then throws
	/// <see cref="OperationCanceledException"/>, as when its token is cancelled. It also cancels the start of the preview that the view makes
	/// when it connects its camera, a change of <see cref="SelectedCamera"/> that still waits for the platform and, on Android, the restart of
	/// the preview for a new capture resolution and the start that the activity would retry once the app has the camera permission (see
	/// <see cref="StartCameraPreview(CancellationToken)"/>). None of the cancelled work is reported as a failure. The preview stays stopped
	/// until <see cref="StartCameraPreview(CancellationToken)"/> is called. On Android, iOS, Mac Catalyst and Windows a start of a video
	/// recording turns the camera on while it records (see <see cref="StartVideoRecording(Stream, CancellationToken)"/>), and on Android a new
	/// extension mode binds the camera again.
	/// </remarks>
	void StopCameraPreview();

	/// <summary>
	/// Starts recording video and writes the output to a <see cref="MemoryStream"/>
	/// </summary>
	/// <remarks>Ensure that the stream is properly disposed of after the recording is complete. The recording will be cancelled if cancellation token is triggered. Call <see cref="ICameraView.StopVideoRecording"/> to return the recorded <see cref="Stream"/>.
	/// A start while <see cref="StopVideoRecording(CancellationToken)"/> waits for the platform to finish the previous recording waits until that
	/// stop has returned its video (Android, iOS, Mac Catalyst and Windows).
	/// <para>The returned task completes once the platform has started the recording (Android, iOS and Mac Catalyst). A start that arrives while
	/// an earlier start still waits for the platform joins it: it returns when that recording started, the stream that it received gets no
	/// video, and when the earlier start fails, the later start throws the same failure. A start after a recording that has started replaces
	/// that recording: the video goes to the new stream, and the recording that ran before ends and its video is discarded. On Windows
	/// the camera stays on in between, and the stream of the recording that ended goes back to the length and the position it had when that
	/// recording started, because Media Foundation writes into the stream while it records. On Android, iOS and Mac Catalyst, when the camera is
	/// released while a start waits (for example because the handler of the view is disconnected), the start throws <see cref="TaskCanceledException"/>.</para>
	/// <para>On Android, iOS, Mac Catalyst and Windows a start while the camera preview is not running (before <see cref="StartCameraPreview(CancellationToken)"/>,
	/// or after <see cref="StopCameraPreview"/>) turns the camera on for the recording, and the camera is off again once the recording ends.
	/// On iOS, Mac Catalyst and Windows, when the camera cannot start (for example without a camera or without the camera permission), the start
	/// throws the failure, and a preview that was stopped stays stopped. On Android a start without a camera returns without recording, and a
	/// recording without the camera permission records no video: <see cref="StopVideoRecording(CancellationToken)"/> then throws a
	/// <see cref="CameraException"/>.</para>
	/// <para><see cref="StopCameraPreview"/> cancels a start that has not completed yet, like a cancellation of <paramref name="token"/>: the recording
	/// does not start, and the start throws <see cref="OperationCanceledException"/>, also when the stop made the start fail. The
	/// <see cref="OperationCanceledException.CancellationToken"/> of the exception is <paramref name="token"/> only when <paramref name="token"/>
	/// cancelled the start.</para></remarks>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	/// <exception cref="CameraException">Thrown when the recording cannot start, for example on iOS and Mac Catalyst when the camera permission is
	/// not granted, or on Windows when no camera is available or the camera cannot be opened. Exceptions are not caught: they propagate to the
	/// caller.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown on Windows by a start that turns the camera on when the privacy settings block the
	/// camera.</exception>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is cancelled, or <see cref="StopCameraPreview"/> is
	/// called, before the recording started.</exception>
	Task StartVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Starts recording video and writes the output to the specified stream.
	/// </summary>
	/// <remarks>Be sure to properly disposed of the provided <see cref="Stream"/> parameter after the recording has completed. The recording will stop if the cancellation token is triggered.
	/// A start while <see cref="StopVideoRecording(CancellationToken)"/> waits for the platform to finish the previous recording waits until that
	/// stop has returned its video (Android, iOS, Mac Catalyst and Windows).
	/// <para>The returned task completes once the platform has started the recording (Android, iOS and Mac Catalyst). A start that arrives while
	/// an earlier start still waits for the platform joins it: it returns when that recording started, the stream that it received gets no
	/// video, and when the earlier start fails, the later start throws the same failure. A start after a recording that has started replaces
	/// that recording: the video goes to the new stream, and the recording that ran before ends and its video is discarded. On Windows
	/// the camera stays on in between, and the stream of the recording that ended goes back to the length and the position it had when that
	/// recording started, because Media Foundation writes into the stream while it records. On Android, iOS and Mac Catalyst, when the camera is
	/// released while a start waits (for example because the handler of the view is disconnected), the start throws <see cref="TaskCanceledException"/>.</para>
	/// <para>On Android, iOS, Mac Catalyst and Windows a start while the camera preview is not running (before <see cref="StartCameraPreview(CancellationToken)"/>,
	/// or after <see cref="StopCameraPreview"/>) turns the camera on for the recording, and the camera is off again once the recording ends.
	/// On iOS, Mac Catalyst and Windows, when the camera cannot start (for example without a camera or without the camera permission), the start
	/// throws the failure, and a preview that was stopped stays stopped. On Android a start without a camera returns without recording, and a
	/// recording without the camera permission records no video: <see cref="StopVideoRecording(CancellationToken)"/> then throws a
	/// <see cref="CameraException"/>.</para>
	/// <para><see cref="StopCameraPreview"/> cancels a start that has not completed yet, like a cancellation of <paramref name="token"/>: the recording
	/// does not start, and the start throws <see cref="OperationCanceledException"/>, also when the stop made the start fail. The
	/// <see cref="OperationCanceledException.CancellationToken"/> of the exception is <paramref name="token"/> only when <paramref name="token"/>
	/// cancelled the start.</para></remarks>
	/// <param name="stream">The stream to which the video data will be written. The stream must be writable and remain open for the duration of the recording.</param>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	/// <exception cref="CameraException">Thrown when the recording cannot start, for example on iOS and Mac Catalyst when the camera permission is
	/// not granted, or on Windows when no camera is available or the camera cannot be opened. Exceptions are not caught: they propagate to the
	/// caller.</exception>
	/// <exception cref="UnauthorizedAccessException">Thrown on Windows by a start that turns the camera on when the privacy settings block the
	/// camera.</exception>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is cancelled, or <see cref="StopCameraPreview"/> is
	/// called, before the recording started.</exception>
	Task StartVideoRecording(Stream stream, CancellationToken token = default);

	/// <summary>
	/// Stops the ongoing video recording operation.
	/// </summary>
	/// <remarks>This method is called to terminate the video recording process. Ensure that either <see cref="StartVideoRecording(CancellationToken)"/> or <see cref="StartVideoRecording(Stream, CancellationToken)"/> has been called before invoking this method. The operation can be cancelled by passing a cancellation token.
	/// <para>The token only cancels waiting for the video, not the stop itself (Android, iOS, Mac Catalyst and Windows): a token that is cancelled
	/// already, or while the stop waits for the video, still stops a recording that runs, and the stop throws <see cref="OperationCanceledException"/>
	/// instead of returning the video. The next <see cref="StopVideoRecording(CancellationToken)"/> returns the video. A token that is cancelled
	/// while a start or an earlier stop is still pending throws without stopping anything, and the start goes on. With no recording and nothing
	/// pending, a cancelled token returns <see cref="Stream.Null"/>.</para></remarks>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation. Its result is the stream that received the video, or
	/// <see cref="Stream.Null"/> when no recording runs (for example on a second call after the recording was stopped); on Android, iOS and Mac
	/// Catalyst also when the camera is released (for example because the page closed) while the stop waits for the platform to finish the
	/// recording.</returns>
	/// <exception cref="OperationCanceledException">Thrown when <paramref name="token"/> is cancelled while a recording runs or a start or an earlier
	/// stop is pending. The recording that runs has stopped all the same, and the next stop returns its video.</exception>
	/// <exception cref="CameraException">Thrown on Android, iOS and Mac Catalyst when the platform reports that the recording failed and wrote no
	/// usable video, for example because it stopped before any frame was recorded; nothing is written to the stream then. When the platform ended
	/// the recording early but the video recorded until then is usable (for example after an interruption, or when the storage filled up), the
	/// method returns that video instead. Exceptions are not caught: they propagate to the caller.</exception>
	Task<Stream> StopVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Retrieves the cameras available on the current device.
	/// </summary>
	/// <remarks>
	/// On Android, when CameraX cannot initialize (for example when the camera service lists fewer cameras than the device declares), the
	/// exception in which CameraX reports it propagates to the caller (see <see cref="ICameraProvider.RefreshAvailableCameras(CancellationToken)"/>).
	/// </remarks>
	/// <param name="token"></param>
	/// <returns></returns>
	ValueTask<IReadOnlyList<CameraInfo>> GetAvailableCameras(CancellationToken token);

	/// <summary>
	/// Occurs when an image is captured by the camera.
	/// </summary>
	/// <param name="imageData">The image data held within a <see cref="Stream"/>.</param>
	internal void OnMediaCaptured(Stream imageData);

	/// <summary>
	/// Occurs when an image capture fails.
	/// </summary>
	/// <param name="failureReason">A string containing the reason why the capture attempt failed.</param>
	internal void OnMediaCapturedFailed(string failureReason);

	/// <summary>
	/// Occurs when a general camera error occurred.
	/// </summary>
	/// <param name="ex">exception with details.</param>
	internal void OnErrorOccurred(Exception ex);
}