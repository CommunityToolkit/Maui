using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

// The attached properties and the extension methods of the platform configuration are plain logic, the same on every platform: their tests
// are unit tests (CommunityToolkit.Maui.UnitTests, PlatformConfiguration/NavigationBarTests.cs).
[Category(TestCategory.NavigationBar)]
[Collection(TestCollections.DisableParallelization)]
public partial class NavigationBarTests : StatusBarTestBase
{
	static ContentPage CreatePage() => new() { Content = new Label { Text = nameof(NavigationBarTests) } };

#if !ANDROID
	[Fact]
	public async Task RemapForControlsRegistersNoMappingsOutsideOfAndroid()
	{
		SetupBuilder();

		Assert.DoesNotContain(NavigationBar.ColorProperty.PropertyName, PageHandler.Mapper.GetKeys());
		Assert.DoesNotContain(NavigationBar.StyleProperty.PropertyName, PageHandler.Mapper.GetKeys());

		var page = CreatePage();
		NavigationBar.SetColor(page, Colors.Red);
		NavigationBar.SetStyle(page, NavigationBarStyle.DarkContent);
		var handler = await InvokeOnMainThreadAsync(() => CreateHandler<PageHandler>(page));

		await InvokeOnMainThreadAsync(() =>
		{
			NavigationBar.SetColor(page, Colors.Blue);
			NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);
		});

		Assert.Same(handler, page.Handler);
		Assert.Equal(Colors.Blue, NavigationBar.GetColor(page));
		Assert.Equal(NavigationBarStyle.LightContent, NavigationBar.GetStyle(page));
	}
#endif
}