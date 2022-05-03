using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class DoubleToIntConverterPage : BasePage<DoubleToIntConverterViewModel>
{
	public DoubleToIntConverterPage(IDeviceInfo deviceInfo, DoubleToIntConverterViewModel doubleToIntConverterViewModel)
		: base(doubleToIntConverterViewModel)
	{
		InitializeComponent();

		ExampleText ??= new();
	}
}