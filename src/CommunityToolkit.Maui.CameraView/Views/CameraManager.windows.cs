using Microsoft.UI.Xaml.Controls;
using Windows.Media.Capture.Frames;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using CommunityToolkit.Maui.Core.Primitives;

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

    protected virtual partial void PlatformConnect()
    {
        if (cameraProvider.AvailableCameras.Count < 1)
        {
            throw new InvalidOperationException("There's no camera available on your device.");
        }
        PlatformStart();
    }

    protected virtual async partial void PlatformStart()
    {
        if (currentCamera is null || mediaElement is null)
        {
            return;
        }

        mediaCapture = new MediaCapture();
        await mediaCapture.InitializeAsync(new MediaCaptureInitializationSettings
        {
            VideoDeviceId = currentCamera.DeviceId,
            PhotoCaptureSource = PhotoCaptureSource.Photo
        });

        UpdateCameraInfo();

        frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord).Value;

        if (frameSource != null)
        {
            mediaElement.AutoPlay = true;
            mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
        }

        Initialized = true;

        await PlatformUpdateResolution(cameraView.CaptureResolution);

        Loaded?.Invoke();
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
        Initialized = false;
    }

    protected void UpdateCameraInfo()
    {
        if (mediaCapture is null || currentCamera is null || currentCamera.Updated)
        {
            return;
        }

        currentCamera.IsFlashSupported = mediaCapture.VideoDeviceController.FlashControl.Supported;
        currentCamera.MinZoomFactor = mediaCapture.VideoDeviceController.ZoomControl.Supported ? mediaCapture.VideoDeviceController.ZoomControl.Min : 1f;
        currentCamera.MaxZoomFactor = mediaCapture.VideoDeviceController.ZoomControl.Supported ? mediaCapture.VideoDeviceController.ZoomControl.Max : 1f;

        var mediaEncodingPropertiesList = mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.Photo)
            .Where(p => p is ImageEncodingProperties).OrderByDescending(p => ((ImageEncodingProperties)p).Width * ((ImageEncodingProperties)p).Height);

        foreach (var mediaEncodingProperties in mediaEncodingPropertiesList)
        {
            var imageEncodingProperties = (ImageEncodingProperties)mediaEncodingProperties;
			if (currentCamera.SupportedResolutions.Contains(new(imageEncodingProperties.Width, imageEncodingProperties.Height)))
			{
				continue;
			}
            currentCamera.ImageEncodingProperties.Add(imageEncodingProperties);
            currentCamera.SupportedResolutions.Add(new(imageEncodingProperties.Width, imageEncodingProperties.Height));
        }

        currentCamera.Updated = true;
    }

    protected async Task PlatformUpdateResolution(Size resolution)
    {
        if (!Initialized || mediaCapture is null || currentCamera is null || !currentCamera.Updated)
        {
            return;
        }

        var filteredPropertiesList = currentCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height);

        filteredPropertiesList = filteredPropertiesList.Any() ? filteredPropertiesList : currentCamera.ImageEncodingProperties
            .OrderByDescending(p => p.Width * p.Height);

        if (filteredPropertiesList.Any())
        {
            await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First());
        }
    }

    public async partial void UpdateCaptureResolution(Size resolution)
    {
        await PlatformUpdateResolution(resolution);
    }

    protected virtual partial void PlatformDisconnect() { }

    protected virtual async partial void PlatformTakePicture()
    {
        if (mediaCapture is null)
        {
            return;
        }

        MemoryStream memoryStream = new();
        await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());
        memoryStream.Position = 0;

        cameraView.OnMediaCaptured(memoryStream);
    }

    public partial void UpdateFlashMode(CameraFlashMode flashMode)
    {
        if (!Initialized || mediaCapture is null || !mediaCapture.VideoDeviceController.FlashControl.Supported)
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
        if (!Initialized || mediaCapture is null || !mediaCapture.VideoDeviceController.ZoomControl.Supported)
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

    protected virtual void Dispose(bool disposing)
    {
        PlatformStop();
        if (disposing)
        {
            mediaCapture?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

}
