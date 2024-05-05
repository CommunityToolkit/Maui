using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Sample.ViewModels.Views;
using CommunityToolkit.Maui.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class CameraViewPage : BasePage<CameraViewModel>
{
	static readonly string imagePath = Path.Combine(FileSystem.CacheDirectory, "camera-view-image.jpg");

	int pageCount;

	public CameraViewPage(CameraViewModel viewModel) : base(viewModel)
	{
		InitializeComponent();

		Camera.MediaCaptured += OnMediaCaptured;

		Loaded += (s, e) =>
		{
			pageCount = Navigation.NavigationStack.Count;
		};
	}

	public ICollection<CameraFlashMode> FlashModes { get; } = Enum.GetValues<CameraFlashMode>();

	void Cleanup()
	{
		Camera.MediaCaptured -= OnMediaCaptured;
		Camera.Handler?.DisconnectHandler();
	}

	void OnUnloaded(object? sender, EventArgs e)
	{
		//Cleanup();
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

	void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
	{
		using FileStream localFileStream = File.Create(imagePath);

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