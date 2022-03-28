using CommunityToolkit.Maui.Sample.ViewModels.Converters;
using Microsoft.Maui.Dispatching;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ByteArrayToImageSourceConverterPage : BasePage<ByteArrayToImageSourceConverterViewModel>
{
	readonly IDispatcher dispatcher;

	public ByteArrayToImageSourceConverterPage(IDeviceInfo deviceInfo, IDispatcher dispatcher, ByteArrayToImageSourceConverterViewModel byteArrayToImageSourceConverterViewModel)
		: base(deviceInfo, byteArrayToImageSourceConverterViewModel)
	{
		InitializeComponent();
		BindingContext.ImageDownloadFailed += HandleImageDownloadFailed;

		this.dispatcher = dispatcher;
	}

	async void HandleImageDownloadFailed(object? sender, string e)
	{
		ArgumentNullException.ThrowIfNull(sender);
		await dispatcher.DispatchAsync(() => DisplayAlert("Image Download Failed", e, "OK"));
	}
}