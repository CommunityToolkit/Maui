using System.Runtime.Versioning;
using Android.Content;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Runtime;
using AndroidX.Camera.Camera2.InterOp;
using AndroidX.Camera.Core;
using AndroidX.Camera.Lifecycle;
using AndroidX.Core.Content;
using Java.Lang;

namespace CommunityToolkit.Maui.Core;

[SupportedOSPlatform("Android21.0")]
partial class CameraProvider
{
	readonly Context context = Android.App.Application.Context;

	private async partial ValueTask PlatformRefreshAvailableCameras(CancellationToken token)
	{
		var cameraProviderFuture = ProcessCameraProvider.GetInstance(context);

		var cameraFutureTCS = new TaskCompletionSource();

		// The listener runs later on the UI thread, outside of this method: an exception that escaped from it would terminate the app, so its
		// outcome completes the refresh instead. Get() throws when CameraX cannot initialize, for example when the camera service lists fewer
		// cameras than the device declares
		cameraProviderFuture.AddListener(new Runnable(() =>
		{
			try
			{
				var processCameraProvider = (ProcessCameraProvider)(cameraProviderFuture.Get() ?? throw new CameraException($"Unable to retrieve {nameof(ProcessCameraProvider)}"));
				var availableCameras = new List<CameraInfo>();

				foreach (var cameraXInfo in processCameraProvider.AvailableCameraInfos)
				{
					var camera2Info = Camera2CameraInfo.From(cameraXInfo);
					if (camera2Info is null)
					{
						// `Camera2CameraInfo.From` should never return `null`
						// According to the Android Docs, `Camera2CameraInfo.From` returns a `NonNull`
						// `Camera2CameraInfo.From` returning a nullable `Camera2CameraInfo` object is likely a C# binding mistake
						// https://developer.android.com/reference/androidx/camera/camera2/interop/Camera2CameraInfo
						continue;
					}

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

						if (OperatingSystem.IsAndroidVersionAtLeast(23))
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

					// JavaCast, not a C# cast: nothing references the binding of CameraX's ZoomState implementation, so trimming removes it. In a
					// trimmed (Release) app the value is then a plain Java.Lang.Object, which a cast to IZoomState turns into null, so every camera
					// reported a zoom range of 1 to 1 (https://github.com/CommunityToolkit/Maui/issues/2389).
					var zoomState = cameraXInfo.ZoomState?.Value?.JavaCast<IZoomState>();

					var cameraInfo = new CameraInfo(name,
						camera2Info.CameraId ?? throw new InvalidOperationException("Unable to retrieve Camera ID"),
						position,
						cameraXInfo.HasFlashUnit,
						zoomState?.MinZoomRatio ?? 1.0f,
						zoomState?.MaxZoomRatio ?? 1.0f,
						supportedResolutions,
						cameraXInfo.CameraSelector ?? throw new InvalidOperationException($"Unable to retrieve {nameof(ICameraInfo.CameraSelector)}"));

					availableCameras.Add(cameraInfo);
				}

				AvailableCameras = availableCameras;

				cameraFutureTCS.SetResult();
			}
			catch (System.Exception ex)
			{
				cameraFutureTCS.TrySetException(ex);
			}
		}), ContextCompat.GetMainExecutor(context));

		await cameraFutureTCS.Task.WaitAsync(token);
	}
}