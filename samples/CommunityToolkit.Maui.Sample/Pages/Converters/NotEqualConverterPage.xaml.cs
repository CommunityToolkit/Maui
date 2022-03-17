using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class NotEqualConverterPage : BasePage<NotEqualConverterViewModel>
{
	public NotEqualConverterPage(IDeviceInfo deviceInfo, NotEqualConverterViewModel notEqualConverterViewModel)
		: base(deviceInfo, notEqualConverterViewModel)
	{
		InitializeComponent();
	}
}