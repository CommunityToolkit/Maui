using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.Maui.Handlers;
using AndroidX.Camera.View;
using Microsoft.Maui;

namespace CommunityToolkit.Maui.Core.Handlers.CameraView;

public partial class CameraViewHandler : ViewHandler<ICameraView, PreviewView>
{
	CameraManager? cameraManager;

	public static IPropertyMapper<ICameraView, CameraViewHandler> Propertymapper = new PropertyMapper<ICameraView, CameraViewHandler>(ViewMapper)
	{

	};

	public static CommandMapper<ICameraView, CameraViewHandler> Commandmapper = new CommandMapper<ICameraView, CameraViewHandler>(ViewCommandMapper)
	{
		[nameof(ICameraView.Shutter)] = MapShutter,
	};

	public static void MapShutter(CameraViewHandler handler, ICameraView view, object? arg3)
	{
		handler.cameraManager?.TakePicture();
	}

	protected override PreviewView CreatePlatformView()
	{
		cameraManager = new CameraManager(MauiContext!, CameraLocation.Front);
		return cameraManager.CreatePlatformView();
	}

	public CameraViewHandler() : base(Propertymapper)
	{
	}
}

