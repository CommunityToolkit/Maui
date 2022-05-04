using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class EnumToIntConverterPage : BasePage<EnumToIntConverterViewModel>
{
	public EnumToIntConverterPage(EnumToIntConverterViewModel enumToIntConverterViewModel)
		: base(enumToIntConverterViewModel)
	{
		InitializeComponent();
	}
}