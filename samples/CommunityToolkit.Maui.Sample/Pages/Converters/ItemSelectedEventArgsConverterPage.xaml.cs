using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ItemSelectedEventArgsConverterPage : BasePage<ItemSelectedEventArgsConverterViewModel>
{
	public ItemSelectedEventArgsConverterPage(ItemSelectedEventArgsConverterViewModel itemSelectedEventArgsConverterViewModel)
		: base(itemSelectedEventArgsConverterViewModel)
	{
		InitializeComponent();
	}
}