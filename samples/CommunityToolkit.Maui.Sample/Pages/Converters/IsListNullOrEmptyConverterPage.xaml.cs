using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsListNullOrEmptyConverterPage : BasePage<IsListNullOrEmptyConverterViewModel>
{
	public IsListNullOrEmptyConverterPage(IDeviceInfo deviceInfo, IsListNullOrEmptyConverterViewModel isListNullOrEmptyConverterViewModel)
		: base(deviceInfo, isListNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}