using Microsoft.Maui.Handlers;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Core.Views.CameraView;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraViewHandler : ViewHandler<ICameraView, UIView>, IDisposable
{
	public static Action<byte[]>? Picture { get; set; }
	
	CameraManager? cameraManager;

	public static IPropertyMapper<ICameraView, CameraViewHandler> Propertymapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
	{
		[nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
		[nameof(IAvailability.IsAvailable)] = MapIsAvailable
	};

	public static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
	{
		var cameraAvailability = (IAvailability)handler.VirtualView;
		cameraAvailability.UpdateAvailability();
	}

	public static CommandMapper<ICameraView, CameraViewHandler> Commandmapper = new(ViewCommandMapper)
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

	protected override UIView CreatePlatformView()
	{
		ArgumentNullException.ThrowIfNull(MauiContext);

		void Init(ICameraView view)
		{
			MapCameraFlashMode(this, view);
		}

		cameraManager = new(MauiContext, CameraLocation.Rear, VirtualView)
		{
			Loaded = () => Init(VirtualView)
		};

		return cameraManager.CreatePlatformView();
	}


	private protected override async void OnConnectHandler(UIView platformView)
	{
		base.OnConnectHandler(platformView);
		await cameraManager!.CheckPermissions();
		cameraManager?.Connect();
	}

	public CameraViewHandler() : base(Propertymapper, Commandmapper)
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