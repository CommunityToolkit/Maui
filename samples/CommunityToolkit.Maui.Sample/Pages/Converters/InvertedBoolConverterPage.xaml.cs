using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class InvertedBoolConverterPage : BasePage<InvertedBoolConverterViewModel>
{
	public InvertedBoolConverterPage(IDeviceInfo deviceInfo, InvertedBoolConverterViewModel invertedBoolConverterViewModel)
		: base(deviceInfo, invertedBoolConverterViewModel)
	{
		InitializeComponent();
	}
}