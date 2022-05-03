using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class StringToListConverterPage : BasePage<StringToListConverterViewModel>
{
	public StringToListConverterPage(IDeviceInfo deviceInfo, StringToListConverterViewModel stringToListConverterViewModel)
		: base(stringToListConverterViewModel)
	{
		InitializeComponent();
	}
}