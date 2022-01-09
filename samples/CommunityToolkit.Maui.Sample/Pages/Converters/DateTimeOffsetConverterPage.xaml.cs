using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class DateTimeOffsetConverterPage : BasePage<DateTimeOffsetConverterViewModel>
{
	public DateTimeOffsetConverterPage(DateTimeOffsetConverterViewModel dateTimeOffsetConverterViewModel)
		: base(dateTimeOffsetConverterViewModel)
	{
		InitializeComponent();
	}
}