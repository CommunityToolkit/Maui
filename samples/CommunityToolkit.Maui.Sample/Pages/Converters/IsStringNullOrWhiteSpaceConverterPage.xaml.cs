using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsStringNullOrWhiteSpaceConverterPage : BasePage<IsStringNullOrWhiteSpaceConverterViewModel>
{
	public IsStringNullOrWhiteSpaceConverterPage(IDeviceInfo deviceInfo, IsStringNullOrWhiteSpaceConverterViewModel isStringNullOrWhiteSpaceConverterViewModel)
		: base(deviceInfo, isStringNullOrWhiteSpaceConverterViewModel)
	{
		InitializeComponent();
	}
}