using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class CompareConverterPage : BasePage<CompareConverterViewModel>
{
	public CompareConverterPage(IDeviceInfo deviceInfo, CompareConverterViewModel compareConverterViewModel)
		: base(deviceInfo, compareConverterViewModel)
	{
		InitializeComponent();
	}
}