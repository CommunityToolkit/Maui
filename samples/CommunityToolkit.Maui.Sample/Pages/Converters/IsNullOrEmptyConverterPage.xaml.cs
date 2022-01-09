using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNullOrEmptyConverterPage : BasePage<IsNullOrEmptyConverterViewModel>
{
	public IsNullOrEmptyConverterPage(IsNullOrEmptyConverterViewModel isNullOrEmptyConverterViewModel)
		: base(isNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}