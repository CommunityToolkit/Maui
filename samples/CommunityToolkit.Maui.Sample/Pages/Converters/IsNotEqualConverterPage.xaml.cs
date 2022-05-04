using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IsNotEqualConverterPage : BasePage<IsNotEqualConverterViewModel>
{
	public IsNotEqualConverterPage(IsNotEqualConverterViewModel notEqualConverterViewModel)
		: base(notEqualConverterViewModel)
	{
		InitializeComponent();
	}
}