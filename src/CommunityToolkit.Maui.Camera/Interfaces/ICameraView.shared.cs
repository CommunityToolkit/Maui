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
	Task<Stream> CaptureImage(CancellationToken token);

	/// <summary>
	/// Starts the camera preview.
	/// </summary>
	/// <remarks>
	/// To customize the behavior of starting the camera preview, consider overriding the behavior through
	/// <c>CameraViewHandler.CommandMapper.ReplaceMapping(nameof(ICameraView.StartCameraPreview), ADD YOUR METHOD);</c>.
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
	/// <remarks>Ensure that the stream is properly disposed of after the recording is complete. The recording will be cancelled if cancellation token is triggered. Call <see cref="ICameraView.StopVideoRecording"/> to return the recorded <see cref="Stream"/></remarks>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	Task StartVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Starts recording video and writes the output to the specified stream.
	/// </summary>
	/// <remarks>Be sure to properly disposed of the provided <see cref="Stream"/> parameter after the recording has completed. The recording will stop if the cancellation token is triggered.</remarks>
	/// <param name="stream">The stream to which the video data will be written. The stream must be writable and remain open for the duration of the recording.</param>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A task that represents the asynchronous video recording operation.</returns>
	Task StartVideoRecording(Stream stream, CancellationToken token = default);

	/// <summary>
	/// Stops the ongoing video recording operation.
	/// </summary>
	/// <remarks>This method is called to terminate the video recording process. Ensure that either <see cref="StartVideoRecording(CancellationToken)"/> or <see cref="StartVideoRecording(Stream, CancellationToken)"/> has been called before invoking this method. The operation can be cancelled by passing a cancellation token.</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	Task<Stream> StopVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Retrieves the cameras available on the current device.
	/// </summary>
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
}