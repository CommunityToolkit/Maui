using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Microsoft.Maui.Handlers;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific.NavigationBar: the page handler mappings that
// UseMauiCommunityToolkit registers on Android (NavigationBar.android.cs), and the empty registration of every other platform.
// The attached properties and the extension methods of the platform configuration are plain logic, the same on every platform: their
// tests are unit tests (CommunityToolkit.Maui.UnitTests, PlatformConfiguration/NavigationBarTests.cs). The Android partial drives the
// mappings and reads the native window (navigation bar color, overlay view, light/dark icons).
[Category(TestCategory.NavigationBar)]
[Collection(TestCollections.DisableParallelization)]
public partial class NavigationBarTests : StatusBarTestBase
{
	static ContentPage CreatePage() => new() { Content = new Label { Text = nameof(NavigationBarTests) } };

#if !ANDROID
	// NavigationBar.RemapForControls does nothing outside of Android: the page handler keeps its mappings, and a page that has the
	// attached properties set is created without anything reacting to them.
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