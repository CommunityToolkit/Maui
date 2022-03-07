using CommunityToolkit.Maui.Sample.ViewModels;
using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class EnumToBoolConverterPage : BasePage<EnumToBoolConverterViewModel>
{
	public EnumToBoolConverterPage(EnumToBoolConverterViewModel enumToBoolConverterViewModel)
		: base(enumToBoolConverterViewModel)
	{
		InitializeComponent();
	}
}