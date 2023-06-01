using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Maui.Maps;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class BasicMapsPage : BasePage<BasicMapsViewModel>
{
	public BasicMapsPage(BasicMapsViewModel mapsViewModel) : base(mapsViewModel) => InitializeComponent();

	private void MapTypePicker_OnSelectedIndexChanged(object? sender, EventArgs e)
	{
		BasicMap.MapType = (MapType)MapTypePicker.SelectedIndex;
	}

	private void Button_OnClicked(object? sender, EventArgs e)
	{
		BasicMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(50, 6), Distance.FromKilometers(1)));
	}
}