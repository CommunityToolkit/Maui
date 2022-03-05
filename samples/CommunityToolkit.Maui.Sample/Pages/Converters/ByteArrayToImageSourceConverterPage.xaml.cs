using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ByteArrayToImageSourceConverterPage : BasePage<ByteArrayToImageSourceConverterViewModel>
{
	public ByteArrayToImageSourceConverterPage(ByteArrayToImageSourceConverterViewModel byteArrayToImageSourceConverterViewModel)
		: base(byteArrayToImageSourceConverterViewModel)
	{
		InitializeComponent();
		BindingContext.ImageDownloadFailed += HandleImageDownloadFailed;
	}

	async void HandleImageDownloadFailed(object? sender, string e) =>
		await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert("Image Download Failed", e, "OK"));
}

