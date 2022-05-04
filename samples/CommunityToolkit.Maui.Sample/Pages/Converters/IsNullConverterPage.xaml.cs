using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNullConverterPage : BasePage<IsNullConverterViewModel>
{
	public IsNullConverterPage(IsNullConverterViewModel isNullConverterViewModel)
		: base(isNullConverterViewModel)
	{
		InitializeComponent();
	}
}