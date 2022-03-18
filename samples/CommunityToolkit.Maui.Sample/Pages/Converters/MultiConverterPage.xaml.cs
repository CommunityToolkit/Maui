using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class MultiConverterPage : BasePage<MultiConverterViewModel>
{
	public MultiConverterPage(IDeviceInfo deviceInfo, MultiConverterViewModel multiConverterViewModel)
		: base(deviceInfo, multiConverterViewModel)
	{
		InitializeComponent();
	}
}