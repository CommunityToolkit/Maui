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
				await Shell.Current.CurrentPage.DisplayAlertAsync("Microphone permission is not granted.", "Please grant the permission to use this feature.", "OK");
				return;
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

	void ZoomIn(object? sender, EventArgs? e)
	{
		Camera.ZoomFactor += 1.0f;
	}

	void ZoomOut(object? sender, EventArgs? e)
	{
		Camera.ZoomFactor -= 1.0f;
	}

	async void SetNightMode(object? sender, EventArgs? e)
	{
#if ANDROID
		await Camera.SetExtensionMode(AndroidX.Camera.Extensions.ExtensionMode.Night);
#else
		await Task.CompletedTask;
#endif
	}

	async void StartCameraRecording(object? sender, EventArgs? e)
	{
		await Camera.StartVideoRecording(CancellationToken.None);
	}

	async void StopCameraRecording(object? sender, EventArgs? e)
	{
		videoRecordingStream = await Camera.StopVideoRecording(CancellationToken.None);
	}

	async void SaveVideo(object? sender, EventArgs? e)
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

			await fileSaver.SaveAsync("recording.mp4", videoRecordingStream);
			await videoRecordingStream.DisposeAsync();
			videoRecordingStream = Stream.Null;
		}
	}
}