using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;

namespace CommunityToolkit.Maui.Sample.Pages;

public class BaseNavigationPage : Microsoft.Maui.Controls.NavigationPage
{
	public BaseNavigationPage(Microsoft.Maui.Controls.Page page) : base(page)
	{
		On<Microsoft.Maui.Controls.PlatformConfiguration.iOS>().SetPrefersLargeTitles(true);
	}
}