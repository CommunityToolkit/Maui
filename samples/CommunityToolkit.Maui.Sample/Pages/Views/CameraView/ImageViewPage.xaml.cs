using Microsoft.Maui.Graphics.Platform;
using IImage = Microsoft.Maui.Graphics.IImage;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class ImageViewPage : ContentPage
{
	public ImageViewPage(string imagePath)
	{
		InitializeComponent();

		Dispatcher.Dispatch(() =>
		{
			// workaround for https://github.com/dotnet/maui/issues/13858
#if ANDROID
            image.Source = ImageSource.FromStream(() => File.OpenRead(imagePath));
#else
			image.Source = ImageSource.FromFile(imagePath);
#endif

			using FileStream stream = File.OpenRead(imagePath);

			// https://learn.microsoft.com/en-us/dotnet/maui/user-interface/graphics/images

			IImage img = PlatformImage.FromStream(stream);

			debugText.Text = $"Resolution: {img.Width} x {img.Height}\nPath: {imagePath}";
		});

	}

	async void NavigateBack(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}
}