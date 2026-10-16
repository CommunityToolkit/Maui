using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public sealed partial class CameraViewPage : BasePage<CameraViewViewModel>
{
	readonly IFileSaver fileSaver;
	readonly string imagePath;

	Stream videoRecordingStream = Stream.Null;

	public CameraViewPage(CameraViewViewModel viewModel, IFileSystem fileSystem, IFileSaver fileSaver) : base(viewModel)
	{
		InitializeComponent();

		this.fileSaver = fileSaver;
		imagePath = Path.Combine(fileSystem.CacheDirectory, "camera-view-image.jpg");

		Camera.MediaCaptured += OnMediaCaptured;
		Camera.ErrorOccurred += OnCameraErrorOccurred;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var cameraPermissionsRequest = await Permissions.RequestAsync<Permissions.Camera>();
		if (cameraPermissionsRequest is not PermissionStatus.Granted)
		{
			await Shell.Current.CurrentPage.DisplayAlertAsync("Camera permission is not granted.", "Please grant the permission to use this feature.", "OK");
			return;
		}

		try
		{
			var microphonePermissionsRequest = await Permissions.RequestAsync<Permissions.Microphone>();
			if (microphonePermissionsRequest is not PermissionStatus.Granted)
			{
				await Shell.Current.CurrentPage.DisplayAlertAsync("Microphone permission is not granted.", "Audio recording will not be available without the Microphone permission, and captured video will be silent.", "OK");
			}
		}
		catch (FileNotFoundException) when (OperatingSystem.IsWindows()) // Unpackaged Windows Apps do not generate the required file AppxManifest.xml 
		{
			await Shell.Current.CurrentPage.DisplayAlertAsync("Unable to access AppxManifest.xml", "Publish using a Packaged .NET MAUI app on Windows to enable Microphone.", "OK");
		}
	}

	// https://github.com/dotnet/maui/issues/15833
	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);

		if (!Shell.Current.Navigation.NavigationStack.Contains(this))
		{
			Cleanup();
		}
	}

	async void OnImageTapped(object? sender, TappedEventArgs args)
	{
		if (image.Source is null || image.Source.IsEmpty)
		{
			return;
		}

		await Navigation.PushAsync(new ImageViewPage(imagePath));
	}

	void Cleanup()
	{
		Camera.MediaCaptured -= OnMediaCaptured;
		Camera.ErrorOccurred -= OnCameraErrorOccurred;
	}

	void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
	{
		try
		{
			using var capturedImageStream = new MemoryStream();
			e.Media.CopyTo(capturedImageStream);

			var imageBytes = capturedImageStream.ToArray();

			using (var localFileStream = new FileStream(imagePath, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				localFileStream.Write(imageBytes, 0, imageBytes.Length);
				localFileStream.Flush(flushToDisk: true);
			}

			Dispatcher.Dispatch(() =>
			{
				image.Source = ImageSource.FromStream(() => new MemoryStream(imageBytes));
				debugText.Text = $"Image saved to {imagePath}";
			});
		}
		catch (Exception ex)
		{
			Dispatcher.Dispatch(() =>
			{
				debugText.Text = $"Failed to save image: {ex.Message}";
			});
		}
	}

	// ErrorOccurred reports the failures of the work that CameraView starts on its own, which no caller could catch: connecting the camera when the
	// page opens (for example on a device without a camera), switching to the camera picked in the Cameras picker, applying the picked resolution
	async void OnCameraErrorOccurred(object? sender, ErrorOccurredEventArgs e)
	{
		await Dispatcher.DispatchAsync(() => DisplayCameraError("Camera Error", e.Exception));
	}

	void ZoomIn(object? sender, EventArgs? e)
	{
		Camera.ZoomFactor += 1.0f;
	}

	void ZoomOut(object? sender, EventArgs? e)
	{
		Camera.ZoomFactor -= 1.0f;
	}

	// The camera methods below throw when the camera fails, for example when the device has no camera, the preview was not started, a permission
	// was not granted, or the page closed during a capture. An exception that escapes an async void event handler ends the app: catch them
	async void SetNightMode(object? sender, EventArgs? e)
	{
#if ANDROID
		try
		{
			await Camera.SetExtensionMode(AndroidX.Camera.Extensions.ExtensionMode.Night);
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Set Night Mode", ex);
		}
#else
		await Task.CompletedTask;
#endif
	}

	async void StartCameraPreview(object? sender, EventArgs? e)
	{
		try
		{
			await Camera.StartCameraPreview(CancellationToken.None);
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Start Camera Preview", ex);
		}
	}

	async void CaptureImage(object? sender, EventArgs? e)
	{
		try
		{
			// OnMediaCaptured shows the captured image, so the returned stream is not needed
			await Camera.CaptureImage(CancellationToken.None);
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Capture Image", ex);
		}
	}

	async void StartCameraRecording(object? sender, EventArgs? e)
	{
		try
		{
			await Camera.StartVideoRecording(CancellationToken.None);
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Start Video Recording", ex);
		}
	}

	async void StopCameraRecording(object? sender, EventArgs? e)
	{
		try
		{
			var recordingStream = await Camera.StopVideoRecording(CancellationToken.None);

			// StopVideoRecording returns Stream.Null when no recording runs, for example when Stop Video Recording is tapped twice:
			// keep the recording that was not saved yet
			if (recordingStream != Stream.Null)
			{
				await videoRecordingStream.DisposeAsync();
				videoRecordingStream = recordingStream;
			}
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Stop Video Recording", ex);
		}
	}

	async void SaveVideo(object? sender, EventArgs? e)
	{
		try
		{
			if (videoRecordingStream == Stream.Null)
			{
				await DisplayAlertAsync("Unable to Save Video", "Stream is null", "OK");
			}
			else
			{
				var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
				if (status is not PermissionStatus.Granted)
				{
					await Shell.Current.CurrentPage.DisplayAlertAsync("Storage permission is not granted.", "Please grant the permission to use this feature.", "OK");
					return;
				}

				var fileSaverResult = await fileSaver.SaveAsync("recording.mp4", videoRecordingStream);
				if (!fileSaverResult.IsSuccessful)
				{
					// The recording is kept, so it can be saved again. A save that the user cancelled needs no message
					if (!fileSaverResult.IsCancelled)
					{
						await DisplayAlertAsync("Unable to Save Video", fileSaverResult.Exception.Message, "OK");
					}

					return;
				}

				await videoRecordingStream.DisposeAsync();
				videoRecordingStream = Stream.Null;
			}
		}
		catch (Exception ex)
		{
			await DisplayCameraError("Unable to Save Video", ex);
		}
	}

	// The failure of work that still ran when the page closed, for example a capture that fails with "Camera is closed" because the CameraView
	// released the camera, shows no alert: DisplayAlertAsync shows nothing for a page that is no longer in a window
	Task DisplayCameraError(string title, Exception exception) => DisplayAlertAsync(title, exception.Message, "OK");
}