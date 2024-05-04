using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Views;
using CommunityToolkit.Maui.Extensions;

namespace CommunityToolkit.Maui.Core.Handlers;

public class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>, IDisposable
{
    public static IPropertyMapper<ICameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
    {
        [nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
        [nameof(IAvailability.IsAvailable)] = MapIsAvailable,
        [nameof(ICameraView.ZoomFactor)] = MapZoomFactor,
        [nameof(ICameraView.CaptureResolution)] = MapCaptureResolution,
        [nameof(ICameraView.SelectedCamera)] = MapSelectedCamera
    };
	
	public static CommandMapper<ICameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(ICameraView.Shutter)] = MapShutter,
		[nameof(ICameraView.Start)] = MapStart,
		[nameof(ICameraView.Stop)] = MapStop
	};

	readonly CameraProvider cameraProvider = IPlatformApplication.Current?.Services.GetRequiredService<CameraProvider>() ?? throw new InvalidOperationException($"{nameof(CameraProvider)} not found");
	
	CameraManager? cameraManager; 
	
	/// <summary>
	/// Initializes a new instance of the <see cref="CameraViewHandler	"/> class.
	/// </summary>
	public CameraViewHandler() : base(PropertyMapper, CommandMapper)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CameraViewHandler"/> class
	/// with custom property and command mappers.
	/// </summary>
	/// <param name="mapper">The custom property mapper to use.</param>
	/// <param name="commandMapper">The custom command mapper to use.</param>
	public CameraViewHandler(IPropertyMapper? mapper, CommandMapper? commandMapper)
		: base(mapper ?? PropertyMapper, commandMapper ?? CommandMapper)
	{

	}
	
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

    protected override NativePlatformCameraPreviewView CreatePlatformView()
    {
        ArgumentNullException.ThrowIfNull(MauiContext);
		cameraManager = new(MauiContext, VirtualView, cameraProvider, () => Init(VirtualView));
		
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
        await (cameraManager?.ArePermissionsGranted() ?? Task.CompletedTask);
        await (cameraManager?.Connect(CancellationToken.None) ?? ValueTask.CompletedTask);
    }

    protected override void DisconnectHandler(NativePlatformCameraPreviewView platformView)
    {
        base.DisconnectHandler(platformView);
        Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            cameraManager?.Dispose();
			cameraManager = null;
		}
    }
	
	static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
	{
		var cameraAvailability = (IAvailability)handler.VirtualView;

#if ANDROID
        cameraAvailability.UpdateAvailability(handler.Context);
#else
		cameraAvailability.UpdateAvailability();
#endif
	}

	static async void MapShutter(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		await (handler.cameraManager?.TakePicture(CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static async void MapStart(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		await (handler.cameraManager?.Start(CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static async void MapCaptureResolution(CameraViewHandler handler, ICameraView view)
	{
		await (handler.cameraManager?.UpdateCaptureResolution(view.CaptureResolution, CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static async void MapSelectedCamera(CameraViewHandler handler, ICameraView view)
	{
		await (handler.cameraManager?.UpdateCurrentCamera(view.SelectedCamera, CancellationToken.None) ?? ValueTask.CompletedTask);
	}

	static void MapStop(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		handler.cameraManager?.Stop();
	}

	static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
	{
		handler.cameraManager?.UpdateFlashMode(view.CameraFlashMode);
	}

	static void MapZoomFactor(CameraViewHandler handler, ICameraView view)
	{
		handler.cameraManager?.UpdateZoom(view.ZoomFactor);
	}
}