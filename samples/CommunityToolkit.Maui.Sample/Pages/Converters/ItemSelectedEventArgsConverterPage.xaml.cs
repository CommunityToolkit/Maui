using CommunityToolkit.Maui.Sample.ViewModels.Converters;

namespace CommunityToolkit.Maui.Sample.Pages.Converters;

public partial class ItemSelectedEventArgsConverterPage : BasePage<ItemSelectedEventArgsConverterViewModel>
{
	public ItemSelectedEventArgsConverterPage(IDeviceInfo deviceInfo, ItemSelectedEventArgsConverterViewModel itemSelectedEventArgsConverterViewModel)
		: base(deviceInfo, itemSelectedEventArgsConverterViewModel)
	{
		InitializeComponent();
	}
}