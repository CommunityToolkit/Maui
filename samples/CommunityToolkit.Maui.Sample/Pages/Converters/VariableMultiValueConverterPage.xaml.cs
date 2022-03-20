using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class VariableMultiValueConverterPage : BasePage<VariableMultiValueConverterViewModel>
{
	public VariableMultiValueConverterPage(IDeviceInfo deviceInfo, VariableMultiValueConverterViewModel variableMultiValueConverterViewModel)
		: base(deviceInfo, variableMultiValueConverterViewModel)
	{
		InitializeComponent();
	}
}