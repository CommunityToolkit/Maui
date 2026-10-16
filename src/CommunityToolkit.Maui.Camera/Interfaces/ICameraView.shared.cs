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
	/// Changing it while the camera preview runs restarts the preview with the new camera. When the new camera cannot be started (for example
	/// because another app uses it), the preview stays stopped, the view reports the failure through its ErrorOccurred event, and selecting
	/// another camera starts the preview with that camera.
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
	/// On Windows, a camera that cannot be started (none is available, or Media Foundation cannot open the selected camera, for example
	/// because another app uses it or the camera permission was removed) is reported through the ErrorOccurred event of the view instead of
	/// an exception.
	/// </remarks>
	Task StartCameraPreview(CancellationToken token);

	/// <summary>
	/// Stops the camera preview.
	/// </summary>
	/// <remarks>
	/// To customize the behavior of stopping the camera preview, consider overriding the behavior through
	/// <c>CameraViewHandler.CommandMapper.ReplaceMapping(nameof(ICameraView.StopCameraPreview), ADD YOUR METHOD);</c>.
	/// </remarks>
	void StopCameraPreview();

	/// <summary>
	/// Starts recording video and writes the output to a <see cref="MemoryStream"/>
	/// </summary>
	/// <remarks>Ensure that the stream is properly disposed of after the recording is complete. The recording will be cancelled if cancellation token is triggered. Call <see cref="ICameraView.StopVideoRecording"/> to return the recorded <see cref="Stream"/>.
	/// A start while <see cref="StopVideoRecording(CancellationToken)"/> waits for the platform to finish the previous recording waits until that
	/// stop has returned its video (Android, iOS and Mac Catalyst).</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	/// <exception cref="CameraException">Thrown when the recording cannot start, for example on iOS and Mac Catalyst when the camera permission is
	/// not granted. Exceptions are not caught: they propagate to the caller.</exception>
	Task StartVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Starts recording video and writes the output to the specified stream.
	/// </summary>
	/// <remarks>Be sure to properly disposed of the provided <see cref="Stream"/> parameter after the recording has completed. The recording will stop if the cancellation token is triggered.
	/// A start while <see cref="StopVideoRecording(CancellationToken)"/> waits for the platform to finish the previous recording waits until that
	/// stop has returned its video (Android, iOS and Mac Catalyst).</remarks>
	/// <param name="stream">The stream to which the video data will be written. The stream must be writable and remain open for the duration of the recording.</param>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	/// <exception cref="CameraException">Thrown when the recording cannot start, for example on iOS and Mac Catalyst when the camera permission is
	/// not granted. Exceptions are not caught: they propagate to the caller.</exception>
	Task StartVideoRecording(Stream stream, CancellationToken token = default);

	/// <summary>
	/// Stops the ongoing video recording operation.
	/// </summary>
	/// <remarks>This method is called to terminate the video recording process. Ensure that either <see cref="StartVideoRecording(CancellationToken)"/> or <see cref="StartVideoRecording(Stream, CancellationToken)"/> has been called before invoking this method. The operation can be cancelled by passing a cancellation token.</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation. Its result is the stream that received the video, or
	/// <see cref="Stream.Null"/> when no recording runs (for example on a second call after the recording was stopped); on Android, iOS and Mac
	/// Catalyst also when the camera is released (for example because the page closed) while the stop waits for the platform to finish the
	/// recording.</returns>
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