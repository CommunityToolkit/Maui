using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IndexToArrayItemConverterPage : BasePage<IndexToArrayItemConverterViewModel>
{
	public IndexToArrayItemConverterPage(IDeviceInfo deviceInfo, IndexToArrayItemConverterViewModel indexToArrayItemConverterViewModel)
		: base(deviceInfo, indexToArrayItemConverterViewModel)
	{
		InitializeComponent();
		Stepper ??= new();
	}
}