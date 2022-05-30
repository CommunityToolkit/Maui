using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.Maui.Handlers;
using AndroidX.Camera.View;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Core.Handlers;

public partial class CameraViewHandler : ViewHandler<ICameraView, PreviewView>
{
	CameraManager? cameraManager;

	public static IPropertyMapper<ICameraView, CameraViewHandler> Propertymapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
	{
		[nameof(ICameraView.CameraFlashMode)] = MapCameraFlashMode,
		[nameof(IAvailability.IsAvailable)] = MapIsAvailable
	};

	public static void MapIsAvailable(CameraViewHandler handler, ICameraView view)
	{
		var cameraAvailability = (IAvailability)handler.VirtualView;
		cameraAvailability.UpdateAvailability(handler.Context);
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

	protected override PreviewView CreatePlatformView()
	{
		cameraManager = new CameraManager(MauiContext!, CameraLocation.Front);
		cameraManager.Loaded = () => Init(VirtualView);
		return cameraManager.CreatePlatformView();
	}

	void Init(ICameraView view)
	{
		MapCameraFlashMode(this, view);
	}

	private protected override async void OnConnectHandler(View platformView)
	{
		base.OnConnectHandler(platformView);
		await cameraManager!.CheckPermissions();
		cameraManager?.Connect();
		Init(this.VirtualView);
	}

	public CameraViewHandler() : base(Propertymapper, Commandmapper)
	{
	}
}

