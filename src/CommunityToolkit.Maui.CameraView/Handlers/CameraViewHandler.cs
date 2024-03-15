#if IOS || ANDROID || WINDOWS

using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>, IDisposable
{
    public static Action<byte[]>? Picture { get; set; }

    CameraManager? cameraManager;

    public static IPropertyMapper<ICameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
    {
        [nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
        [nameof(IAvailability.IsAvailable)] = MapIsAvailable,
        [nameof(ICameraView.ZoomFactor)] = MapZoomFactor,
        [nameof(ICameraView.CaptureResolution)] = MapCaptureResolution,
        [nameof(ICameraView.SelectedCamera)] = MapSelectedCamera
    };

    public static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
    {
        var cameraAvailability = (IAvailability)handler.VirtualView;

#if IOS || WINDOWS
		cameraAvailability.UpdateAvailability();
#elif ANDROID
        cameraAvailability.UpdateAvailability(handler.Context);
#endif
    }

    public static CommandMapper<ICameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
    {
        [nameof(ICameraView.Shutter)] = MapShutter,
        [nameof(ICameraView.Start)] = MapStart,
        [nameof(ICameraView.Stop)] = MapStop
    };

    public static void MapShutter(CameraViewHandler handler, ICameraView view, object? arg3)
    {
        handler.cameraManager?.TakePicture();
    }

    public static void MapStart(CameraViewHandler handler, ICameraView view, object? arg3)
    {
        handler.cameraManager?.Start();
    }

    public static void MapStop(CameraViewHandler handler, ICameraView view, object? arg3)
    {
        handler.cameraManager?.Stop();
    }

    public static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
    {
        handler.cameraManager?.UpdateFlashMode(view.CameraFlashMode);
    }

    public static void MapZoomFactor(CameraViewHandler handler, ICameraView view)
    {
        handler.cameraManager?.UpdateZoom(view.ZoomFactor);
    }

    public static void MapCaptureResolution(CameraViewHandler handler, ICameraView view)
    {
        handler.cameraManager?.UpdateCaptureResolution(view.CaptureResolution);
    }

    public static void MapSelectedCamera(CameraViewHandler handler, ICameraView view)
    {
        handler.cameraManager?.UpdateCurrentCamera(view.SelectedCamera);
    }

    protected override NativePlatformCameraPreviewView CreatePlatformView()
    {
        ArgumentNullException.ThrowIfNull(MauiContext);
        cameraManager = new(MauiContext, VirtualView)
        {
            Loaded = () => Init(VirtualView)
        };
        return cameraManager.CreatePlatformView();

		// When camera is loaded(switched), map the current flash mode to the platform view,
		// reset the zoom factor to 1
        void Init(ICameraView view)
        {
            MapCameraFlashMode(this, view);
            view.ZoomFactor = 1.0f;
        }
    }

    protected override async void ConnectHandler(NativePlatformCameraPreviewView platformView)
    {
        base.ConnectHandler(platformView);
        await cameraManager!.CheckPermissions();
        cameraManager?.Connect();
    }

    protected override void DisconnectHandler(NativePlatformCameraPreviewView platformView)
    {
        base.DisconnectHandler(platformView);
        Dispose();
    }

    public CameraViewHandler() : base(PropertyMapper, CommandMapper)
    {
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            cameraManager?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

#endif
