using CommunityToolkit.Maui.Sample.ViewModels.Views;
using Microsoft.Maui.Maps;

namespace CommunityToolkit.Maui.Sample.Pages.Views;

public partial class BasicMapsPage : BasePage<BasicMapsViewModel>
{
	public BasicMapsPage(BasicMapsViewModel mapsViewModel) : base(mapsViewModel) => InitializeComponent();

	protected override void OnNavigatedTo(NavigatedToEventArgs args)
	{
		base.OnNavigatedTo(args);

		basicMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(50, 6), Distance.FromKilometers(1)));
	}

}