using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

/// <summary>
/// A class that manages the camera functionality.
/// </summary>
/// <remarks>
/// Creates a new instance of the <see cref="CameraManager"/> class.
/// </remarks>
/// <param name="mauiContext">The <see cref="IMauiContext"/> implementation.</param>
/// <param name="cameraView">The <see cref="ICameraView"/> implementation.</param>
/// <param name="cameraProvider">The <see cref="CameraProvider"/> implementation.</param>
/// <param name="onLoaded">The <see cref="Action"/> to execute when the camera is loaded.</param>
/// <exception cref="NullReferenceException">Thrown when no <see cref="CameraProvider"/> can be resolved.</exception>
/// <exception cref="InvalidOperationException">Thrown when there are no cameras available.</exception>
partial class CameraManager(
	IMauiContext mauiContext,
	ICameraView cameraView,
	ICameraProvider cameraProvider,
	Action onLoaded) : IDisposable
{
	internal Action OnLoaded { get; } = onLoaded;

	internal bool IsInitialized { get; private set; }

	/// <summary>
	/// Whether the required permissions have been granted by the user through the use of the <see cref="Permissions"/> API.
	/// </summary>
	/// <returns>Returns <c>true</c> if permission has been granted, <c>false</c> otherwise.</returns>
	public async Task<bool> ArePermissionsGranted()
		=> await Permissions.RequestAsync<Permissions.Camera>() is PermissionStatus.Granted;

	/// <summary>
	/// Connects to the camera.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public Task ConnectCamera(CancellationToken token) => PlatformConnectCamera(token);

	/// <summary>
	/// Disconnects from the camera.
	/// </summary>
	public void Disconnect() => PlatformDisconnect();

	/// <summary>
	/// Takes a picture using the camera.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public ValueTask TakePicture(CancellationToken token) => PlatformTakePicture(token);

	/// <summary>
	/// Starts the camera preview.
	/// </summary>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public Task StartCameraPreview(CancellationToken token) => PlatformStartCameraPreview(token);

	/// <summary>
	/// Stops the camera preview.
	/// </summary>
	public void StopCameraPreview() => PlatformStopCameraPreview();

	/// <summary>
	/// Updates the current camera.
	/// </summary>
	/// <param name="cameraInfo">The new <see cref="CameraInfo"/> to set as current.</param>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <remarks>
	/// If the <see cref="CameraManager"/> is initialized the old camera preview will be stopped
	/// and the new camera preview will be started.
	/// </remarks>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public async ValueTask UpdateCurrentCamera(CameraInfo? cameraInfo, CancellationToken token)
	{
		if (cameraInfo is null)
		{
			return;
		}

		if (IsInitialized)
		{
			PlatformStopCameraPreview();
			await PlatformStartCameraPreview(token);
		}
	}

	/// <summary>
	/// Updates the flash mode of the camera.
	/// </summary>
	/// <param name="flashMode">The new <see cref="CameraFlashMode"/> to set.</param>
	public partial void UpdateFlashMode(CameraFlashMode flashMode);

	/// <summary>
	/// Updates the zoom level of the camera.
	/// </summary>
	/// <param name="zoomLevel">The new zoom level to set.</param>
	public partial void UpdateZoom(float zoomLevel);

	/// <summary>
	/// Updates the capture resolution of the camera.
	/// </summary>
	/// <param name="resolution">The <see cref="Size" /> resolution to use when capturing an image from the current camera.</param>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token);

	/// <summary>
	/// Performs the capturing of a picture at the platform specific level. 
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="ValueTask"/> that can be awaited.</returns>
	protected virtual partial ValueTask PlatformTakePicture(CancellationToken token);

	/// <summary>
	/// Starts the preview from the camera, at the platform specific level.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	protected virtual partial Task PlatformStartCameraPreview(CancellationToken token);

	/// <summary>
	/// Connects to the camera, at the platform specific level.
	/// </summary>
	/// <param name="token">A <see cref="CancellationToken"/> that can be used to cancel the work.</param>
	/// <returns>A <see cref="Task"/> that can be awaited.</returns>
	protected virtual partial Task PlatformConnectCamera(CancellationToken token);

	/// <summary>
	/// Disconnects from the camera, at the platform specific level.
	/// </summary>
	protected virtual partial void PlatformDisconnect();

	/// <summary>
	/// Stops the preview from the camera, at the platform specific level.
	/// </summary>
	protected virtual partial void PlatformStopCameraPreview();
}