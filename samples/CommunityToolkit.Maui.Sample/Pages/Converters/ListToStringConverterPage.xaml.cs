using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ListToStringConverterPage : BasePage<ListToStringConverterViewModel>
{
	public ListToStringConverterPage(ListToStringConverterViewModel listToStringConverterViewModel)
		: base(listToStringConverterViewModel)
	{
		InitializeComponent();
	}
}