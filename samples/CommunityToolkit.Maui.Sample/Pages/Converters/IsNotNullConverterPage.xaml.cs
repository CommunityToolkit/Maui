using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNotNullConverterPage : BasePage<IsNotNullConverterViewModel>
{
	public IsNotNullConverterPage(IsNotNullConverterViewModel isNotNullConverterViewModel)
		: base(isNotNullConverterViewModel)
	{
		InitializeComponent();
	}
}