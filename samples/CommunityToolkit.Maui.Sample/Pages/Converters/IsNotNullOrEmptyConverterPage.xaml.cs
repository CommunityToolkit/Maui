using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNotNullOrEmptyConverterPage : BasePage<IsNotNullOrEmptyConverterViewModel>
{
	public IsNotNullOrEmptyConverterPage(IsNotNullOrEmptyConverterViewModel isNotNullOrEmptyConverterViewModel)
		: base(isNotNullOrEmptyConverterViewModel)
	{
		InitializeComponent();
	}
}