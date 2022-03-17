using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ImageResourceConverterPage : BasePage<ImageResourceConverterViewModel>
{
	public ImageResourceConverterPage(IDeviceInfo deviceInfo, ImageResourceConverterViewModel imageResourceConverterViewModel)
		: base(deviceInfo, imageResourceConverterViewModel)
	{
		InitializeComponent();
	}
}