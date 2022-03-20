using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class DateTimeOffsetConverterPage : BasePage<DateTimeOffsetConverterViewModel>
{
	public DateTimeOffsetConverterPage(IDeviceInfo deviceInfo, DateTimeOffsetConverterViewModel dateTimeOffsetConverterViewModel)
		: base(deviceInfo, dateTimeOffsetConverterViewModel)
	{
		InitializeComponent();
	}
}