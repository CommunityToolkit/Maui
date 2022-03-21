using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class SelectedItemEventArgsConverterPage : BasePage<SelectedItemEventArgsConverterViewModel>
{
	public SelectedItemEventArgsConverterPage(IDeviceInfo deviceInfo, SelectedItemEventArgsConverterViewModel selectedItemEventArgsConverterViewModel)
		: base(deviceInfo, selectedItemEventArgsConverterViewModel)
	{
		InitializeComponent();
	}
}