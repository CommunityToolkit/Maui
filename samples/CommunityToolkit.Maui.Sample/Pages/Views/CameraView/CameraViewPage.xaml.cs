using System.Diagnostics;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Storage;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public sealed partial class CameraViewPage : BasePage<CameraViewViewModel>
{
	readonly IFileSaver fileSaver;
	readonly string imagePath;

	int pageCount;
	Stream videoRecordingStream = Stream.Null;

	public CameraViewPage(CameraViewViewModel viewModel, IFileSystem fileSystem, IFileSaver fileSaver) : base(viewModel)
	{
		InitializeComponent();

		this.fileSaver = fileSaver;
		imagePath = Path.Combine(fileSystem.CacheDirectory, "camera-view-image.jpg");

		Camera.MediaCaptured += OnMediaCaptured;

		Loaded += (s, e) => { pageCount = Navigation.NavigationStack.Count; };
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(3));
		await BindingContext.RefreshCamerasCommand.ExecuteAsync(cancellationTokenSource.Token);
	}

	// https://github.com/dotnet/maui/issues/16697
	// https://github.com/dotnet/maui/issues/15833
	protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
	{
		base.OnNavigatedFrom(args);

		Debug.WriteLine($"< < OnNavigatedFrom {pageCount} {Navigation.NavigationStack.Count}");

		if (Navigation.NavigationStack.Count < pageCount)
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
		Camera.Handler?.DisconnectHandler();
	}

	void OnUnloaded(object? sender, EventArgs e)
	{
		//Cleanup();
	}

	void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
	{
		using var localFileStream = File.Create(imagePath);

		e.Media.CopyTo(localFileStream);

		Dispatcher.Dispatch(() =>
		{
			// workaround for https://github.com/dotnet/maui/issues/13858
#if ANDROID
            image.Source = ImageSource.FromStream(() => File.OpenRead(imagePath));
#else
			image.Source = ImageSource.FromFile(imagePath);
#endif

			debugText.Text = $"Image saved to {imagePath}";
		});
	}

	void ZoomIn(object? sender, EventArgs e)
	{
		Camera.ZoomFactor += 1.0f;
	}

	void ZoomOut(object? sender, EventArgs e)
	{
		Camera.ZoomFactor -= 1.0f;
	}

	async void SetNightMode(object? sender, EventArgs e)
	{
#if ANDROID
		await Camera.SetExtensionMode(AndroidX.Camera.Extensions.ExtensionMode.Night);
#else
		await Task.CompletedTask;
#endif
	}

	async void StartCameraRecording(object? sender, EventArgs e)
	{
		await Camera.StartVideoRecording(CancellationToken.None);
	}

	async void StopCameraRecording(object? sender, EventArgs e)
	{
		videoRecordingStream = await Camera.StopVideoRecording(CancellationToken.None);
	}

	async void SaveVideo(object? sender, EventArgs e)
	{
		if (videoRecordingStream == Stream.Null)
		{
			await DisplayAlert("Unable to Save Video", "Stream is null", "OK");
		}
		else
		{
			await fileSaver.SaveAsync("recording.mp4", videoRecordingStream);
			await videoRecordingStream.DisposeAsync();
			videoRecordingStream = Stream.Null;
		}
	}
}