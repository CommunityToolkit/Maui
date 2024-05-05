using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Extensions;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace CommunityToolkit.Maui.Core;

partial class CameraManager
{
	MediaPlayerElement? mediaElement;
	MediaCapture? mediaCapture;
	MediaFrameSource? frameSource;

	public MediaPlayerElement CreatePlatformView()
	{
		mediaElement = new MediaPlayerElement();
		return mediaElement;
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	public partial void UpdateFlashMode(CameraFlashMode flashMode)
	{
		if (!IsInitialized || (mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
		{
			return;
		}

		if (mediaCapture is null)
		{
			return;
		}

		var (updatedFlashControlEnabled, updatedFlashControlAuto) = flashMode switch
		{
			CameraFlashMode.Off => (false, (bool?)null),
			CameraFlashMode.On => (true, false),
			CameraFlashMode.Auto => (true, true),
			_ => throw new NotSupportedException($"{flashMode} is not yet supported")
		};

		mediaCapture.VideoDeviceController.FlashControl.Enabled = updatedFlashControlEnabled;

		if (updatedFlashControlAuto.HasValue)
		{
			mediaCapture.VideoDeviceController.FlashControl.Auto = updatedFlashControlAuto.Value;
		}

	}

	public partial void UpdateZoom(float zoomLevel)
	{
		if (!IsInitialized || mediaCapture is null || !mediaCapture.VideoDeviceController.ZoomControl.Supported)
		{
			return;
		}

		var step = mediaCapture.VideoDeviceController.ZoomControl.Step;

		if (zoomLevel % step != 0)
		{
			zoomLevel = (float)Math.Ceiling(zoomLevel / step) * step;
		}

		mediaCapture.VideoDeviceController.ZoomControl.Value = zoomLevel;
	}

	public async partial ValueTask UpdateCaptureResolution(Size resolution, CancellationToken token)
	{
		await PlatformUpdateResolution(resolution, token);
	}

	protected virtual partial void PlatformDisconnect()
	{
	}

	protected virtual async partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		if (mediaCapture is null)
		{
			return;
		}

		token.ThrowIfCancellationRequested();

		MemoryStream memoryStream = new();

		await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());

		memoryStream.Position = 0;

		cameraView.OnMediaCaptured(memoryStream);
	}

	protected virtual void Dispose(bool disposing)
	{
		PlatformStopCameraPreview();
		if (disposing)
		{
			mediaCapture?.Dispose();
		}
	}

	protected virtual partial ValueTask PlatformConnectCamera(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras.Count < 1)
		{
			throw new CameraViewException("No camera available on device");
		}

		return StartCameraPreview(token);
	}

	protected virtual async partial ValueTask PlatformStartCameraPreview(CancellationToken token)
	{
		if (currentCamera is null || mediaElement is null)
		{
			return;
		}

		mediaCapture = new MediaCapture();

		token.ThrowIfCancellationRequested();

		await mediaCapture.InitializeCameraForCameraView(currentCamera.DeviceId, token);

		await UpdateCameraInfo(token);

		frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		IsInitialized = true;

		await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

		OnLoaded.Invoke();
	}

	protected virtual partial void PlatformStopCameraPreview()
	{
		if (mediaElement is null)
		{
			return;
		}

		mediaElement.Source = null;
		mediaCapture?.Dispose();
		mediaCapture = null;
		IsInitialized = false;
	}

	protected ValueTask UpdateCameraInfo(CancellationToken token)
	{
		if (mediaCapture is null)
		{
			return ValueTask.CompletedTask;
		}

		return cameraProvider.RefreshAvailableCameras(token);
	}

	protected async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (!IsInitialized || mediaCapture is null || currentCamera is null)
		{
			return;
		}

		var filteredPropertiesList = currentCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height);

		filteredPropertiesList = filteredPropertiesList.Any() ? filteredPropertiesList : currentCamera.ImageEncodingProperties
			.OrderByDescending(p => p.Width * p.Height);

		if (filteredPropertiesList.Any())
		{
			token.ThrowIfCancellationRequested();
			await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First());
		}
	}
}