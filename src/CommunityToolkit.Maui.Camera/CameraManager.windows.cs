using CommunityToolkit.Maui.Extensions;
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
	LowLagMediaRecording? mediaRecording;
	Stream? videoCaptureStream;

	public MediaPlayerElement CreatePlatformView()
	{
		mediaElement = new MediaPlayerElement();
		return mediaElement;
	}

	public void Dispose()
	{
		PlatformStopCameraPreview();
		mediaCapture?.Dispose();
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

	private partial void PlatformDisconnect()
	{
	}

	private async partial ValueTask PlatformTakePicture(CancellationToken token)
	{
		if (mediaCapture is null)
		{
			return;
		}

		token.ThrowIfCancellationRequested();

		MemoryStream memoryStream = new();

		try
		{
			await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), memoryStream.AsRandomAccessStream());

			memoryStream.Position = 0;

			cameraView.OnMediaCaptured(memoryStream);
		}
		catch (Exception ex)
		{
			cameraView.OnMediaCapturedFailed(ex.Message);
			throw;
		}
	}

	private async partial Task PlatformConnectCamera(CancellationToken token)
	{
		await StartCameraPreview(token);
	}

	private async partial Task PlatformStartCameraPreview(CancellationToken token)
	{
		if (mediaElement is null)
		{
			return;
		}

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault() ?? throw new CameraException("No camera available on device");

		mediaCapture = new MediaCapture();

		await mediaCapture.InitializeCameraForCameraView(cameraView.SelectedCamera.DeviceId, token);

		frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		IsInitialized = true;

		await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

		OnLoaded.Invoke();
	}

	private partial void PlatformStopCameraPreview()
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

	async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (cameraView.SelectedCamera is null || !IsInitialized || mediaCapture is null)
		{
			return;
		}

		var filteredPropertiesList = cameraView.SelectedCamera.ImageEncodingProperties.Where(p => p.Width <= resolution.Width && p.Height <= resolution.Height).ToList();

		if (filteredPropertiesList.Count is 0)
		{
			filteredPropertiesList = [.. cameraView.SelectedCamera.ImageEncodingProperties.OrderByDescending(p => p.Width * p.Height)];
		}

		if (filteredPropertiesList.Count is not 0)
		{
			await mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.Photo, filteredPropertiesList.First()).AsTask(token);
		}
	}

	private async partial Task PlatformStartVideoRecording(Stream stream, CancellationToken token)
	{
		if (!IsInitialized || mediaCapture is null || mediaElement is null)
		{
			return;
		}

		videoCaptureStream = stream;

		var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
		mediaRecording = await mediaCapture.PrepareLowLagRecordToStreamAsync(profile, stream.AsRandomAccessStream());

		frameSource = mediaCapture.FrameSources
			.FirstOrDefault(static source => source.Value.Info.MediaStreamType is MediaStreamType.VideoRecord && source.Value.Info.SourceKind is MediaFrameSourceKind.Color)
			.Value;

		if (frameSource is not null)
		{
			var frameFormat = frameSource.SupportedFormats
					.OrderByDescending(f => f.VideoFormat.Width * f.VideoFormat.Height)
					.FirstOrDefault();

			if (frameFormat is not null)
			{
				await frameSource.SetFormatAsync(frameFormat);
				mediaElement.AutoPlay = true;
				mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
				await mediaRecording.StartAsync();
			}
		}
	}

	private async partial Task<Stream> PlatformStopVideoRecording(CancellationToken token)
	{
		if (!IsInitialized || mediaElement is null || mediaRecording is null || videoCaptureStream is null)
		{
			return Stream.Null;
		}

		await mediaRecording.StopAsync();
		return videoCaptureStream;
	}
}