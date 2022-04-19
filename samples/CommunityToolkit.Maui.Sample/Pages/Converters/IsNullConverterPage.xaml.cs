using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNullConverterPage : BasePage<IsNullConverterViewModel>
{
	public IsNullConverterPage(IDeviceInfo deviceInfo, IsNullConverterViewModel isNullConverterViewModel)
		: base(deviceInfo, isNullConverterViewModel)
	{
		InitializeComponent();
	}
}