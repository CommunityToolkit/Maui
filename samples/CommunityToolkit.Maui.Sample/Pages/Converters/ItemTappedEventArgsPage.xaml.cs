using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ItemTappedEventArgsPage : BasePage<ItemTappedEventArgsViewModel>
{
	public ItemTappedEventArgsPage(ItemTappedEventArgsViewModel itemTappedEventArgsViewModel)
		: base(itemTappedEventArgsViewModel)
	{
		InitializeComponent();
	}
}