using System.Runtime.Versioning;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using CommunityToolkit.Maui.Core.Primitives;
using Java.Lang;

namespace CommunityToolkit.Maui.Core;

[SupportedOSPlatform("android21.0")]
partial class CameraProvider
{
	readonly Context context = Android.App.Application.Context;

	public async partial ValueTask RefreshAvailableCameras(CancellationToken token)
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);

		var cameraFutureTCS = new TaskCompletionSource();

		cameraProviderFuture.AddListener(new Runnable(() =>
		{
			var processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}"));
			var availableCameras = new List<CameraInfo>();

			foreach (var cameraXInfo in processCameraProvider.AvailableCameraInfos)
			{
				var camera2Info = Camera2CameraInfo.From(cameraXInfo);

				var (name, position) = cameraXInfo.LensFacing switch
				{
					CameraSelector.LensFacingBack => ("Back Camera", CameraPosition.Rear),
					CameraSelector.LensFacingFront => ("Front Camera", CameraPosition.Front),
					CameraSelector.LensFacingExternal or CameraSelector.LensFacingUnknown => ("Unknown Position Camera", CameraPosition.Unknown),
					_ => throw new NotSupportedException($"{cameraXInfo.LensFacing} not yet supported")
				};

				var supportedResolutions = new List<Size>();

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
								supportedResolutions.Add(new(r.Width, r.Height));
							}
						}
					}

					var resolutions = streamConfigMap?.GetOutputSizes((int)ImageFormatType.Jpeg);
					if (resolutions is not null)
					{
						foreach (var r in resolutions)
						{
							supportedResolutions.Add(new(r.Width, r.Height));
						}
					}
				}

				var cameraInfo = new CameraInfo(name,
					camera2Info.CameraId,
					position,
					cameraXInfo.HasFlashUnit,
					(cameraXInfo.ZoomState.Value as IZoomState)?.MinZoomRatio ?? 1.0f,
					(cameraXInfo.ZoomState.Value as IZoomState)?.MaxZoomRatio ?? 1.0f,
					supportedResolutions,
					cameraXInfo.CameraSelector);

				availableCameras.Add(cameraInfo);
			}

			AvailableCameras = availableCameras;

			cameraFutureTCS.SetResult();

		}), ContextCompat.GetMainExecutor(context));

		await cameraFutureTCS.Task.WaitAsync(token);
	}
}