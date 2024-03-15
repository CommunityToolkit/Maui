using System.Diagnostics;
using Java.Lang;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using AndroidX.Core.Content;
using AndroidX.Camera.Core;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Lifecycle;
using CommunityToolkit.Maui.Core.Primitives;

namespace CommunityToolkit.Maui.Core;

public partial class CameraProvider
{
    readonly Context context = Android.App.Application.Context;

    public partial void RefreshAvailableCameras()
    {
        AvailableCameras.Clear();

        var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);

        cameraProviderFuture.AddListener(new Runnable(() =>
        {
            var processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new NullReferenceException());

            foreach (var cameraXInfo in processCameraProvider.AvailableCameraInfos)
            {
                var cameraInfo = new CameraInfo();
                var camera2Info = Camera2CameraInfo.From(cameraXInfo);
                cameraInfo.DeviceId = camera2Info.CameraId;

                switch (cameraXInfo.LensFacing)
                {
                    case CameraSelector.LensFacingBack:
                        cameraInfo.Name = "Back Camera";
                        cameraInfo.Position = CameraPosition.Rear;
                        break;
                    case CameraSelector.LensFacingFront:
                        cameraInfo.Name = "Front Camera";
                        cameraInfo.Position = CameraPosition.Front;
                        break;
                    default:
                        cameraInfo.Name = "Unknown Position Camera";
                        cameraInfo.Position = CameraPosition.Unknown;
                        break;
                }

                cameraInfo.IsFlashSupported = cameraXInfo.HasFlashUnit;

				if (cameraXInfo.ZoomState.Value is IZoomState zoomState)
				{
					cameraInfo.MinZoomFactor = zoomState.MinZoomRatio;
					cameraInfo.MaxZoomFactor = zoomState.MaxZoomRatio;
				}

				cameraInfo.CameraSelector = cameraXInfo.CameraSelector;

                if (CameraCharacteristics.ScalerStreamConfigurationMap is not null)
                {
                    var streamConfigMap = camera2Info.GetCameraCharacteristic(CameraCharacteristics.ScalerStreamConfigurationMap) as StreamConfigurationMap;

					if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
					{
						var highResolutions = streamConfigMap?.GetHighResolutionOutputSizes((int)ImageFormatType.Jpeg);
						if (highResolutions is not null)
						{
							foreach (var r in highResolutions)
							{
								cameraInfo.SupportedResolutions.Add(new(r.Width, r.Height));
							}
						}
					}

					var resolutions = streamConfigMap?.GetOutputSizes((int)ImageFormatType.Jpeg);
					if (resolutions is not null)
                    {
                        foreach (var r in resolutions)
                        {
                            cameraInfo.SupportedResolutions.Add(new(r.Width, r.Height));
                        }
                    }
                }

                AvailableCameras.Add(cameraInfo);
            }
        }), ContextCompat.GetMainExecutor(context));
    }

}
