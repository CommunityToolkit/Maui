using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using CommunityToolkit.Maui.Core.Primitives;
using Microsoft.Maui.Controls.PlatformConfiguration;

namespace CommunityToolkit.Maui.Core.Views;

public partial class CameraManager
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
		if (!IsInitialized || mediaCapture is null || (mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
		{
			return;
		}

		switch (flashMode)
		{
			case CameraFlashMode.Off:
				mediaCapture.VideoDeviceController.FlashControl.Enabled = false;
				break;
			case CameraFlashMode.On:
				mediaCapture.VideoDeviceController.FlashControl.Enabled = true;
				mediaCapture.VideoDeviceController.FlashControl.Auto = false;
				break;
			case CameraFlashMode.Auto:
				mediaCapture.VideoDeviceController.FlashControl.Enabled = true;
				mediaCapture.VideoDeviceController.FlashControl.Auto = true;
				break;
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
		PlatformStop();
		if (disposing)
		{
			mediaCapture?.Dispose();
		}
	}

	protected virtual partial ValueTask PlatformConnect(CancellationToken token)
	{
		if (cameraProvider.AvailableCameras.Count < 1)
		{
			throw new InvalidOperationException("There's no camera available on your device.");
		}

		return PlatformStart(token);
	}

	protected virtual async partial ValueTask PlatformStart(CancellationToken token)
	{
		if (currentCamera is null || mediaElement is null)
		{
			return;
		}

		mediaCapture = new MediaCapture();

		token.ThrowIfCancellationRequested();

		await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
		{
			VideoDeviceId = currentCamera.DeviceId,
			PhotoCaptureSource = PhotoCaptureSource.Photo
		});

		await UpdateCameraInfo(token);

		frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		IsInitialized = true;

		await PlatformUpdateResolution(cameraView.CaptureResolution, token);

		OnLoaded.Invoke();
	}

	protected virtual partial void PlatformStop()
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
