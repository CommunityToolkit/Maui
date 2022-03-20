using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsStringNotNullOrEmptyConverterPage : BasePage<IsStringNotNullOrEmptyConverterViewModel>
{
	public IsStringNotNullOrEmptyConverterPage(IDeviceInfo deviceInfo, IsStringNotNullOrEmptyConverterViewModel isStringNotNullOrEmptyConverterViewModel)
		: base(deviceInfo, isStringNotNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}