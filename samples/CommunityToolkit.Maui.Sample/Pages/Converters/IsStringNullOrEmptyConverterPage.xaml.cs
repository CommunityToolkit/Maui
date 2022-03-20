using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsStringNullOrEmptyConverterPage : BasePage<IsStringNullOrEmptyConverterViewModel>
{
	public IsStringNullOrEmptyConverterPage(IDeviceInfo deviceInfo, IsStringNullOrEmptyConverterViewModel isStringNullOrEmptyConverterViewModel)
		: base(deviceInfo, isStringNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}