using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Views;

/// <summary>
/// A class that manages the camera functionality.
/// </summary>
public partial class CameraManager : IDisposable
{
    readonly IMauiContext mauiContext;
    readonly ICameraView cameraView;

	readonly CameraProvider cameraProvider;
    CameraInfo? currentCamera;

    internal bool Initialized { get; private set; } = false;

    internal Action? Loaded { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="CameraManager"/> class.
    /// </summary>
    /// <param name="mauiContext">The <see cref="IMauiContext"/> implementation.</param>
    /// <param name="cameraView">The <see cref="ICameraView"/> implementation.</param>
    /// <exception cref="NullReferenceException">Thrown when no <see cref="CameraProvider"/> can be resolved.</exception>
    /// <exception cref="InvalidOperationException">Thrown when there are no cameras available.</exception>
    public CameraManager(IMauiContext mauiContext, ICameraView cameraView)
    {
        this.mauiContext = mauiContext;
        this.cameraView = cameraView;
        this.cameraProvider = IPlatformApplication.Current?.Services.GetService<CameraProvider>() ?? throw new NullReferenceException();
        this.currentCamera = cameraView.SelectedCamera ??= cameraProvider.AvailableCameras.First() ?? throw new InvalidOperationException("No available camera found.");
    }

    /// <summary>
    /// Whether the required permissions have been granted by the user through the use of the <see cref="Permissions"/> API.
    /// </summary>
    /// <returns>Returns <c>true</c> if permission has been granted, <c>false</c> otherwise.</returns>
    public async Task<bool> CheckPermissions()
        => (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

    /// <summary>
    /// Connects to the camera.
    /// </summary>
    public void Connect() => PlatformConnect();

    /// <summary>
    /// Disconnects from the camera.
    /// </summary>
    public void Disconnect() => PlatformDisconnect();

    /// <summary>
    /// Takes a picture using the camera.
    /// </summary>
    public void TakePicture() => PlatformTakePicture();

    /// <summary>
    /// Starts the camera preview.
    /// </summary>
    public void Start() => PlatformStart();

    /// <summary>
    /// Stops the camera preview.
    /// </summary>
    public void Stop() => PlatformStop();

    /// <summary>
    /// Updates the current camera.
    /// </summary>
    /// <param name="cameraInfo">The new <see cref="CameraInfo"/> to set as current.</param>
    /// <remarks>
    /// If the <see cref="CameraManager"/> is initialized the old camera preview will be stopped
    /// and the new camera preview will be started.
    /// </remarks>
    public void UpdateCurrentCamera(CameraInfo? cameraInfo)
    {
        if (cameraInfo is null || cameraInfo == currentCamera)
        {
            return;
        }

        currentCamera = cameraInfo;

        if (Initialized)
        {
            PlatformStop();
            PlatformStart();
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
    public partial void UpdateCaptureResolution(Size resolution);

    protected virtual partial void PlatformConnect();
    protected virtual partial void PlatformDisconnect();
    protected virtual partial void PlatformTakePicture();
    protected virtual partial void PlatformStart();
    protected virtual partial void PlatformStop();
}

#if !ANDROID && !IOS && !WINDOWS
public partial class CameraManager
{
    protected virtual partial void PlatformConnect() { }
    protected virtual partial void PlatformDisconnect() { }
    protected virtual partial void PlatformTakePicture() { }
    protected virtual partial void PlatformStart() { }
    protected virtual partial void PlatformStop() { }
    public partial void UpdateFlashMode(CameraFlashMode flashMode)
    {
    }

    public partial void UpdateZoom(float zoomLevel)
    {
    }

    public partial void UpdateCaptureResolution(Size resolution)
    {
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="CameraManager"/>.
    /// </summary>
    /// <param name="disposing">Whether the camera manager is being disposed.</param>
    protected virtual void Dispose(bool disposing)
    {
    }

    /// <summary>
    /// Disposes of the resources used by the <see cref="CameraManager"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
#endif
