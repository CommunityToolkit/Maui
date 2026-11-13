using Microsoft.Maui.Graphics.Platform;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ImageViewPage : ContentPage
{
	public ImageViewPage(string imagePath)
	{
		InitializeComponent();

		// workaround for https://github.com/dotnet/maui/issues/13858
#if ANDROID
		image.Source = ImageSource.FromStream(() => File.OpenRead(imagePath));
#else
		image.Source = ImageSource.FromFile(imagePath);
#endif

		var (imageWidth, imageHeight) = GetImageDimensions(imagePath);

		debugLabel.Text = $"Resolution: {imageWidth} x {imageHeight}\nPath: {imagePath}";
	}

	static (double width, double height) GetImageDimensions(in string imagePath)
	{
		using var stream = File.OpenRead(imagePath);

		// https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/images

		var image = PlatformImage.FromStream(stream);

		return (image.Width, image.Height);
	}

	async void NavigateBack(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}