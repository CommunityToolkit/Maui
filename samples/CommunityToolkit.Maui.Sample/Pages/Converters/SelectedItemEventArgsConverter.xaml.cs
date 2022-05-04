using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class SelectedItemEventArgsConverterPage : BasePage<SelectedItemEventArgsConverterViewModel>
{
	public SelectedItemEventArgsConverterPage(SelectedItemEventArgsConverterViewModel selectedItemEventArgsConverterViewModel)
		: base(selectedItemEventArgsConverterViewModel)
	{
		InitializeComponent();
	}
}