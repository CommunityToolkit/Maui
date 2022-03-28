using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsStringNotNullOrWhiteSpaceConverterPage : BasePage<IsStringNotNullOrWhiteSpaceConverterViewModel>
{
	public IsStringNotNullOrWhiteSpaceConverterPage(IDeviceInfo deviceInfo, IsStringNotNullOrWhiteSpaceConverterViewModel isStringNotNullOrWhiteSpaceConverterViewModel)
		: base(deviceInfo, isStringNotNullOrWhiteSpaceConverterViewModel)
	{
		InitializeComponent();
	}
}