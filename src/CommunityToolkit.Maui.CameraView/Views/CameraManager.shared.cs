using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core.Views;

public partial class CameraManager : IDisposable
{
    readonly IMauiContext mauiContext;
    readonly ICameraView cameraView;

    CameraProvider cameraProvider;
    CameraInfo? currentCamera;

    internal bool Initialized { get; private set; } = false;

    internal Action? Loaded { get; set; }

    public CameraManager(IMauiContext mauiContext, ICameraView cameraView)
    {
        this.mauiContext = mauiContext;
        this.cameraView = cameraView;
        this.cameraProvider = IPlatformApplication.Current?.Services.GetService<CameraProvider>() ?? throw new NullReferenceException();
        this.currentCamera = cameraView.SelectedCamera ??= cameraProvider.AvailableCameras.First() ?? throw new InvalidOperationException("No available camera found.");
    }

    public async Task<bool> CheckPermissions()
            => (await Permissions.RequestAsync<Permissions.Camera>()) == PermissionStatus.Granted;

    public void Connect() => PlatformConnect();
    public void Disconnect() => PlatformDisconnect();
    public void TakePicture() => PlatformTakePicture();
    public void Start() => PlatformStart();
    public void Stop() => PlatformStop();

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

    public partial void UpdateFlashMode(CameraFlashMode flashMode);

    public partial void UpdateZoom(float zoomLevel);

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

    protected virtual void Dispose(bool disposing)
    {
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
#endif
