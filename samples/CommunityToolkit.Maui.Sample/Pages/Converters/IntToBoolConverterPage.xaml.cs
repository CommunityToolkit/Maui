using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class IntToBoolConverterPage : BasePage<IntToBoolConverterViewModel>
{
	public IntToBoolConverterPage(IntToBoolConverterViewModel intToBoolConverterViewModel)
		: base(intToBoolConverterViewModel)
	{
		InitializeComponent();
	}
}