#if IOS || MACCATALYST || ANDROID

using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Views.CameraView;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraViewHandler : ViewHandler<ICameraView, NativePlatformCameraPreviewView>, IDisposable
{
	public static Action<byte[]>? Picture { get; set; }
	
	CameraManager? cameraManager;

	public static IPropertyMapper<ICameraView, CameraViewHandler> PropertyMapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
	{
		[nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
		[nameof(IAvailability.IsAvailable)] = MapIsAvailable
	};

	public static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
	{
		var cameraAvailability = (IAvailability)handler.VirtualView;

#if !ANDROID
		cameraAvailability.UpdateAvailability();
#else
		cameraAvailability.UpdateAvailability(handler.Context);
#endif
	}

	public static CommandMapper<ICameraView, CameraViewHandler> CommandMapper = new(ViewCommandMapper)
	{
		[nameof(ICameraView.Shutter)] = MapShutter,
	};

	public static void MapShutter(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		handler.cameraManager?.TakePicture();
	}

	public static void MapCameraFlashMode(CameraViewHandler handler, ICameraView view)
	{
		handler.cameraManager?.UpdateFlashMode(view.CameraFlashMode);
	}

	protected override NativePlatformCameraPreviewView CreatePlatformView()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);
		cameraManager = new(MauiContext, CameraLocation.Rear, VirtualView)
		{
			Loaded = () => Init(VirtualView)
		};
		return cameraManager.CreatePlatformView();

		void Init(ICameraView view)
		{
			MapCameraFlashMode(this, view);
		}
	}

	private protected override async void OnConnectHandler(NativePlatformView platformView)
	{
		base.OnConnectHandler(platformView);
		await cameraManager!.CheckPermissions();
		cameraManager?.Connect();
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