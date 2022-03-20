using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class EqualConverterPage : BasePage<EqualConverterViewModel>
{
	public EqualConverterPage(IDeviceInfo deviceInfo, EqualConverterViewModel equalConverterViewModel)
		: base(deviceInfo, equalConverterViewModel)
	{
		InitializeComponent();
	}
}