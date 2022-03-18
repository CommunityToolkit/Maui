using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ListToStringConverterPage : BasePage<ListToStringConverterViewModel>
{
	public ListToStringConverterPage(IDeviceInfo deviceInfo, ListToStringConverterViewModel listToStringConverterViewModel)
		: base(deviceInfo, listToStringConverterViewModel)
	{
		InitializeComponent();
	}
}