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
	/// Gets a value indicating whether the torch is on.
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
	/// Gets whether the implementation is available.
	/// </summary>
	bool IsAvailable { get; internal set; }

	/// <summary>
	/// Gets whether the implementation is busy.
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
	/// Starts recording video and writes the output to the specified stream.
	/// </summary>
	/// <remarks>Ensure that the stream is properly disposed of after the recording is complete. The recording will
	/// stop if the cancellation token is triggered.</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the video recording operation.</param>
	/// <returns>A <see cref="Task"/> of type <see cref="Stream"/> where the recording will be saved in memory as the video records.</returns>
	Task<Stream> StartVideoRecording(CancellationToken token = default);

	/// <summary>
	/// Stops the ongoing video recording operation.
	/// </summary>
	/// <remarks>This method should be called to terminate the video recording process. Ensure that the recording
	/// has been started before invoking this method. The operation can be cancelled by passing a  cancellation
	/// token.</remarks>
	/// <param name="token">A cancellation token that can be used to cancel the stop operation.</param>
	/// <returns>A task that represents the asynchronous stop operation.</returns>
	Task StopVideoRecording(CancellationToken token = default);

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