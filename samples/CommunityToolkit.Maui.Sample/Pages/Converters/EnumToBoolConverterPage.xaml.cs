using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class EnumToBoolConverterPage : BasePage<EnumToBoolConverterViewModel>
{
	public EnumToBoolConverterPage(IDeviceInfo deviceInfo, EnumToBoolConverterViewModel enumToBoolConverterViewModel)
		: base(deviceInfo, enumToBoolConverterViewModel)
	{
		InitializeComponent();
	}
}