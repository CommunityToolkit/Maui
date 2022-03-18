using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class TextCaseConverterPage : BasePage<TextCaseConverterViewModel>
{
	public TextCaseConverterPage(IDeviceInfo deviceInfo, TextCaseConverterViewModel textCaseConverterViewModel)
		: base(deviceInfo, textCaseConverterViewModel)
	{
		InitializeComponent();
	}
}