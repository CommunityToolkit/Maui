using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsListNotNullOrEmptyConverterPage : BasePage<IsListNotNullOrEmptyConverterViewModel>
{
	public IsListNotNullOrEmptyConverterPage(IDeviceInfo deviceInfo, IsListNotNullOrEmptyConverterViewModel isListNotNullOrEmptyConverterViewModel)
		: base(deviceInfo, isListNotNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}