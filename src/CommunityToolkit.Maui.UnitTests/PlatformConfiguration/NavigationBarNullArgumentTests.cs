using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.PlatformConfiguration.AndroidSpecific;
using Xunit;
using AndroidPlatform = Microsoft.Maui.Controls.PlatformConfiguration.Android;

namespace CommunityToolkit.Maui.UnitTests.PlatformConfiguration;

// NavigationBar is an Android platform configuration, but its attached properties and the extension methods of the platform configuration are plain logic that every platform compiles
#pragma warning disable CS8604, CS8625 // Possible null reference argument; cannot convert null literal to non-nullable reference type.
public class NavigationBarNullArgumentTests : BaseTest
{
	[Fact]
	public void NavigationBar_NullBindable_ShouldThrowArgumentNullException()
	{
		NullArgumentAssert.Throws("bindable", () => NavigationBar.GetColor((BindableObject?)null));
		NullArgumentAssert.Throws("bindable", () => NavigationBar.SetColor((BindableObject?)null, Colors.Red));
	}

	[Fact]
	public void NavigationBar_NullConfiguration_ShouldThrowArgumentNullException()
	{
		IPlatformElementConfiguration<AndroidPlatform, Page>? configuration = null;

		NullArgumentAssert.Throws("config", () => configuration.GetColor());
		NullArgumentAssert.Throws("config", () => configuration.SetColor(Colors.Red));
		NullArgumentAssert.Throws("config", () => configuration.GetStyle());
		NullArgumentAssert.Throws("config", () => configuration.SetStyle(NavigationBarStyle.Default));
	}
}
#pragma warning restore CS8604, CS8625
