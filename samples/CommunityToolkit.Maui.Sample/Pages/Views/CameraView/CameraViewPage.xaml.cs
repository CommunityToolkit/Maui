using System.Diagnostics;
using CommunityToolkit.Maui.Core.Primitives;
using CommunityToolkit.Maui.Sample.ViewModels.Views;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class CameraViewPage : BasePage<CameraViewModel>
{
    int pageCount;

    public CameraViewPage(CameraViewModel viewModel) : base(viewModel)
	{
        InitializeComponent();

		camera.MediaCaptured += OnMediaCaptured;

        Loaded += (s, e) =>
        {
            pageCount = Navigation.NavigationStack.Count;
        };
    }

    string ImagePath { get; set; } = Path.Combine(FileSystem.CacheDirectory, "camera-view-image.jpg");

    public ICollection<CameraFlashMode> FlashModes => Enum.GetValues<CameraFlashMode>();

    void Cleanup()
    {
        camera.MediaCaptured -= OnMediaCaptured;
        camera.Handler?.DisconnectHandler();
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

    void Shutter(object? sender, EventArgs e)
    {
        camera.Shutter();
    }

    void Start(object? sender, EventArgs e)
    {
        camera.Start();
    }

    void Stop(object? sender, EventArgs e)
    {
        camera.Stop();
    }

    async void OnImageTapped(object? sender, TappedEventArgs args)
    {
        if (image.Source is null || image.Source.IsEmpty)
        {
            return;
        }
        await Navigation.PushAsync(new ImageViewPage(ImagePath));
    }

    void OnMediaCaptured(object? sender, MediaCapturedEventArgs e)
    {
        using FileStream localFileStream = File.Create(ImagePath);

        e.Media.CopyTo(localFileStream);

        Dispatcher.Dispatch(() =>
        {
            // workaround for https://github.com/dotnet/maui/issues/13858
#if ANDROID
            image.Source = ImageSource.FromStream(() => File.OpenRead(ImagePath));
#else
            image.Source = ImageSource.FromFile(ImagePath);
#endif

            debugText.Text = $"Image saved to {ImagePath}";
        });

    }

    void ZoomIn(object? sender, EventArgs e)
    {
        camera.ZoomFactor += 1.0f;
    }

    void ZoomOut(object? sender, EventArgs e)
    {
        camera.ZoomFactor -= 1.0f;
    }
}
