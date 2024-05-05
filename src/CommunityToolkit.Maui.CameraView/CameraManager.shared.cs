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
	CameraProvider cameraProvider,
	Action onLoaded) : IDisposable
{
	CameraInfo currentCamera = cameraView.SelectedCamera ??= cameraProvider.AvailableCameras.FirstOrDefault() ?? throw new CameraViewException("No available camera found.");

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
	public ValueTask ConnectCamera(CancellationToken token) => PlatformConnectCamera(token);

	/// <summary>
	/// Disconnects from the camera.
	/// </summary>
	public void Disconnect() => PlatformDisconnect();

	/// <summary>
	/// Takes a picture using the camera.
	/// </summary>
	public ValueTask TakePicture(CancellationToken token) => PlatformTakePicture(token);

	/// <summary>
	/// Starts the camera preview.
	/// </summary>
	public ValueTask StartCameraPreview(CancellationToken token) => PlatformStartCameraPreview(token);

	/// <summary>
	/// Stops the camera preview.
	/// </summary>
	public void StopCameraPreview() => PlatformStopCameraPreview();

	/// <summary>
	/// Updates the current camera.
	/// </summary>
	/// <param name="cameraInfo">The new <see cref="CameraInfo"/> to set as current.</param>
	/// <remarks>
	/// If the <see cref="CameraManager"/> is initialized the old camera preview will be stopped
	/// and the new camera preview will be started.
	/// </remarks>
	public async ValueTask UpdateCurrentCamera(CameraInfo? cameraInfo, CancellationToken token)
	{
		if (cameraInfo is null || cameraInfo == currentCamera)
		{
			return;
		}

		currentCamera = cameraInfo;

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
	public partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token);
	protected virtual partial ValueTask PlatformTakePicture(CancellationToken token);
	protected virtual partial ValueTask PlatformStartCameraPreview(CancellationToken token);
	protected virtual partial ValueTask PlatformConnectCamera(CancellationToken token);
	protected virtual partial void PlatformDisconnect();
	protected virtual partial void PlatformStopCameraPreview();
}