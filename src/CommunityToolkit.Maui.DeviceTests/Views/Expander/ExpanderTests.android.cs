#pragma warning disable CS0618 // ListView and ViewCell are obsolete, but Expander still supports them in a ListView
using Android.Views;
using AListView = Android.Widget.ListView;

namespace CommunityToolkit.Maui.DeviceTests;

// Android needs no platform code in Expander: the platform list measures its rows again by itself.
public partial class ExpanderTests
{
	static bool IsPlatformViewVisible(IView view) => ((Android.Views.View)view.Handler!.PlatformView!).Visibility == ViewStates.Visible;

	static double GetRowHeight(ListView listView, int row) => ToDips(listView, GetRow(listView, row)?.Height ?? 0);

	static double GetRowTop(ListView listView, int row) => ToDips(listView, GetRow(listView, row)?.Top ?? 0);

	// MAUI's ListView adds a header (and footer) container to the platform list, so the first row sits after HeaderViewsCount children.
	static Android.Views.View? GetRow(ListView listView, int row)
	{
		var native = Assert.IsType<AListView>(listView.Handler!.PlatformView);
		return native.GetChildAt(row + native.HeaderViewsCount - native.FirstVisiblePosition);
	}

	static double ToDips(ListView listView, int pixels)
	{
		var native = Assert.IsType<AListView>(listView.Handler!.PlatformView);
		return pixels / native.Context!.Resources!.DisplayMetrics!.Density;
	}
}