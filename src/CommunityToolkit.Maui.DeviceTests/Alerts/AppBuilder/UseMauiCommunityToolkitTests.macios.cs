using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class UseMauiCommunityToolkitTests
{
	[Fact]
	public async Task UseMauiCommunityToolkitDoesNotMapTheNavigationBarPropertiesOnThePageHandler()
	{
		SetupBuilder();

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.Null(PageHandler.Mapper.GetProperty(NavigationBar.ColorProperty.PropertyName));
			Assert.Null(PageHandler.Mapper.GetProperty(NavigationBar.StyleProperty.PropertyName));
		});
	}
}