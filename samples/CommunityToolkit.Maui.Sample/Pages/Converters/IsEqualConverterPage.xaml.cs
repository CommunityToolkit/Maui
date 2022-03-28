using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsEqualConverterPage : BasePage<IsEqualConverterViewModel>
{
	public IsEqualConverterPage(IDeviceInfo deviceInfo, IsEqualConverterViewModel equalConverterViewModel)
		: base(deviceInfo, equalConverterViewModel)
	{
		InitializeComponent();
	}
}