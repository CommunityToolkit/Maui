using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsEqualConverterPage : BasePage<IsEqualConverterViewModel>
{
	public IsEqualConverterPage(IsEqualConverterViewModel equalConverterViewModel)
		: base(equalConverterViewModel)
	{
		InitializeComponent();
	}
}