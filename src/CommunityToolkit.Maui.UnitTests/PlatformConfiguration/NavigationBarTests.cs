using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Xunit;
using AndroidPlatform = Microsoft.Maui.Controls.PlatformConfiguration.Android;

namespace CommunityToolkit.Maui.UnitTests.PlatformConfiguration;

// NavigationBar is an Android platform configuration (SupportedOSPlatform Android 23), but its attached properties and the extension
// methods of the platform configuration are plain logic that every platform compiles. What the Android page handler does with them
// (the navigation bar of the window) is tested by the device tests (CommunityToolkit.Maui.DeviceTests, NavigationBarTests).
#pragma warning disable CA1416 // Validate platform compatibility
public class NavigationBarTests : BaseTest
{
	[Fact]
	public void ColorAndStyleHaveTheirDefaultsBeforeTheyAreSet()
	{
		var page = new ContentPage();
		var configuration = page.On<AndroidPlatform>();

		Assert.Equal(Color.FromUint(0u), configuration.GetColor());
		Assert.Equal(NavigationBarStyle.Default, configuration.GetStyle());
		Assert.Equal(Color.FromUint(0u), NavigationBar.GetColor(page));
		Assert.Equal(NavigationBarStyle.Default, NavigationBar.GetStyle(page));
	}

	// The Android page handler applies Color and Style only when the page has them set (BindableObject.IsSet), so reading them must
	// not count as setting them: the app did not ask for a navigation bar color or style.
	[Fact]
	public void ReadingColorAndStyleDoesNotSetThem()
	{
		var page = new ContentPage();

		_ = NavigationBar.GetColor(page);
		_ = page.On<AndroidPlatform>().GetColor();
		_ = NavigationBar.GetStyle(page);
		_ = page.On<AndroidPlatform>().GetStyle();

		Assert.False(page.IsSet(NavigationBar.ColorProperty));
		Assert.False(page.IsSet(NavigationBar.StyleProperty));
	}

	// A color equal to the default is still a color the app asked for.
	[Fact]
	public void ColorSetToTheDefaultValueIsSet()
	{
		var page = new ContentPage();
		_ = NavigationBar.GetColor(page);

		NavigationBar.SetColor(page, Color.FromUint(0u));

		Assert.True(page.IsSet(NavigationBar.ColorProperty));
		Assert.Equal(Color.FromUint(0u), NavigationBar.GetColor(page));
	}

	[Fact]
	public void ColorAndStyleSetThroughThePlatformConfigurationCanBeReadBack()
	{
		var page = new ContentPage();
		var configuration = page.On<AndroidPlatform>();

		Assert.Same(configuration, configuration.SetColor(Colors.Green));
		Assert.Same(configuration, configuration.SetStyle(NavigationBarStyle.DarkContent));

		Assert.Equal(Colors.Green, configuration.GetColor());
		Assert.Equal(NavigationBarStyle.DarkContent, configuration.GetStyle());
		Assert.Equal(Colors.Green, NavigationBar.GetColor(page));
		Assert.Equal(NavigationBarStyle.DarkContent, NavigationBar.GetStyle(page));
	}

	[Fact]
	public void ColorAndStyleSetOnThePageCanBeReadThroughThePlatformConfiguration()
	{
		var page = new ContentPage();

		NavigationBar.SetColor(page, Colors.Red);
		NavigationBar.SetStyle(page, NavigationBarStyle.LightContent);

		Assert.Equal(Colors.Red, page.On<AndroidPlatform>().GetColor());
		Assert.Equal(NavigationBarStyle.LightContent, page.On<AndroidPlatform>().GetStyle());
	}
}
#pragma warning restore CA1416 // Validate platform compatibility