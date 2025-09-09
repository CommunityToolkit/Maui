using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class CameraViewPage : BasePage<CameraViewViewModel>
{
	readonly string imagePath;

	public CameraViewPage(CameraViewViewModel viewModel, IFileSystem fileSystem) : base(viewModel)
	{
		InitializeComponent();

		imagePath = Path.Combine(fileSystem.CacheDirectory, "camera-view-image.jpg");

		Camera.MediaCaptured += OnMediaCaptured;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		await BindingContext.InitializeAsync();
	}

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
}