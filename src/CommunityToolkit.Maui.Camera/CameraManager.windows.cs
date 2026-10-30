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
		if (!isInitialized || (mediaCapture?.VideoDeviceController.FlashControl.Supported is false))
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

	public partial void UpdateIsTorchOn(bool isTorchOn)
	{
		if (!isInitialized ||
			mediaCapture is null ||
			!(mediaCapture.VideoDeviceController?.TorchControl?.Supported ?? false))
		{
			return;
		}

		bool isCurrentlyOn = mediaCapture.VideoDeviceController.TorchControl.Enabled;

		if (isTorchOn != isCurrentlyOn)
		{
			mediaCapture.VideoDeviceController.TorchControl.Enabled = isTorchOn;
		}
	}

	public partial void UpdateZoom(float zoomLevel)
	{
		if (!isInitialized || mediaCapture is null || !mediaCapture.VideoDeviceController.ZoomControl.Supported)
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
			// CameraView.CaptureImage waits for MediaCaptured or MediaCaptureFailed, so a capture that cannot start must be reported
			cameraView.OnMediaCapturedFailed("Unable to capture an image because the camera is not initialized. Start the camera preview first.");
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
			// Reported like on Android, iOS and Mac Catalyst: CameraView.CaptureImage then throws the CameraException of MediaCaptureFailed
			// (a rethrow here made it throw the platform exception instead and left that CameraException unobserved)
			cameraView.OnMediaCapturedFailed(ex.Message);
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

		cameraView.SelectedCamera ??= cameraProvider.AvailableCameras?.FirstOrDefault();

		if (cameraView.SelectedCamera is null)
		{
			cameraView.OnErrorOccurred(
				new CameraException("Couldn't start camera preview; no cameras available"));
			return;
		}

		var startingMediaCapture = new MediaCapture();
		mediaCapture = startingMediaCapture;

		try
		{
			if (!await startingMediaCapture.InitializeCameraForCameraView(cameraView.SelectedCamera.DeviceId, token))
			{
				throw new CameraException("Unable to open the selected camera. It may be unavailable or in use by another application.");
			}

			// StopCameraPreview cancels a camera switch and releases the MediaCapture that the switch initializes: an initialization that Media
			// Foundation completes all the same must not go on with it
			token.ThrowIfCancellationRequested();
		}
		catch (Exception ex)
		{
			// A MediaCapture that Media Foundation could not initialize cannot preview or capture, whatever stopped its initialization (a camera
			// that cannot be opened, the cancellation of the token, a camera that the privacy settings block). A start that the app calls after
			// the StopCameraPreview that cancelled this one can have replaced it by now: only this start's own MediaCapture is released
			startingMediaCapture.Dispose();
			if (ReferenceEquals(mediaCapture, startingMediaCapture))
			{
				mediaCapture = null;
			}

			if (ex is OperationCanceledException)
			{
				throw;
			}

			// Media Foundation can also fail to initialize the MediaCapture that StopCameraPreview released: a start that was cancelled reports no
			// error
			token.ThrowIfCancellationRequested();

			// A camera that cannot be opened (it does not exist any more, or another app uses it) leaves the preview started, so that selecting
			// another camera starts the preview with that camera (UpdateCurrentCamera). A camera that the privacy settings block
			// (UnauthorizedAccessException) leaves it stopped until StartCameraPreview is called again
			isInitialized = ex is not UnauthorizedAccessException;

			cameraView.OnErrorOccurred(ex);

			return;
		}

		frameSource = mediaCapture.FrameSources.FirstOrDefault(source => source.Value.Info.MediaStreamType == MediaStreamType.VideoRecord && source.Value.Info.SourceKind == MediaFrameSourceKind.Color).Value;

		if (frameSource is not null)
		{
			mediaElement.AutoPlay = true;
			mediaElement.Source = MediaSource.CreateFromMediaFrameSource(frameSource);
		}

		isInitialized = true;

		await PlatformUpdateResolution(cameraView.ImageCaptureResolution, token);

		onLoaded.Invoke();
	}

	private partial void PlatformStopCameraPreviewForCameraSwitch() => PlatformStopCameraPreview();

	private partial void PlatformStopCameraPreview()
	{
		if (mediaElement is null)
		{
			return;
		}

		mediaElement.Source = null;
		mediaCapture?.Dispose();

		mediaCapture = null;
		isInitialized = false;
	}

	async Task PlatformUpdateResolution(Size resolution, CancellationToken token)
	{
		if (cameraView.SelectedCamera is null || !isInitialized || mediaCapture is null)
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
		if (!isInitialized || mediaCapture is null || mediaElement is null)
		{
			return;
		}

		var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.Auto);
		mediaRecording = await mediaCapture.PrepareLowLagRecordToStreamAsync(profile, stream.AsRandomAccessStream());

		// Only a recording that could be prepared replaces the stream: a stream that is refused (AsRandomAccessStream throws for a stream
		// that cannot seek) must not replace the stream of a recording that is running
		videoCaptureStream = stream;

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
		if (!isInitialized || mediaElement is null || mediaRecording is null || videoCaptureStream is null)
		{
			return Stream.Null;
		}

		// The stop ends the recording, as on Android, iOS and Mac Catalyst: a later StopVideoRecording finds no recording and returns
		// Stream.Null instead of stopping the stopped LowLagMediaRecording again, which throws a COMException
		var recording = mediaRecording;
		var stream = videoCaptureStream;
		mediaRecording = null;
		videoCaptureStream = null;

		await recording.StopAsync();

		// Releases the resources of the recording; LowLagMediaRecording cannot be started again after FinishAsync
		await recording.FinishAsync();

		if (stream.CanSeek)
		{
			stream.Position = 0;
		}

		return stream;
	}
}