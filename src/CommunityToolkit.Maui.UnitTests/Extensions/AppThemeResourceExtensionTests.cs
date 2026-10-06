using CommunityToolkit.Maui.UnitTests.Mocks;
using CommunityToolkit.Maui.UnitTests.Resources.Styles;
using Xunit;

namespace CommunityToolkit.Maui.UnitTests.Extensions;

public class AppThemeResourceExtensionTests : BaseViewTest
{
	[Fact]
	public async Task SetPropertyViaAppThemeResource()
	{
		// Arrange
		MockApplication application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		bool foundResource;

		// Act
		try
		{
			application.Resources.MergedDictionaries.Add(new AppThemeResourceDictionary());
			foundResource = true;
		}
		catch (Exception)
		{
			foundResource = false;
		}
		// Assert
		Assert.True(foundResource, "Failed to load key from AppThemeResourceDictionary. Bug 2761.");
	}

	[Fact]
	public void AppThemeResourceInStyleUpdatesWhenThemeChanges()
	{
		var application = (MockApplication)ServiceProvider.GetRequiredService<IApplication>();
		application.UserAppTheme = AppTheme.Light;

		var resources = new AppThemeResourceDictionary();
		var style = Assert.Single(resources.Values.OfType<Style>());
		var firstEntry = new Entry { Style = style };
		var secondEntry = new Entry { Style = style };

		Assert.Equal(Colors.Black, firstEntry.TextColor);
		Assert.Equal(Colors.Black, secondEntry.TextColor);

		application.UserAppTheme = AppTheme.Dark;

		Assert.Equal(Colors.White, firstEntry.TextColor);
		Assert.Equal(Colors.White, secondEntry.TextColor);

		application.UserAppTheme = AppTheme.Light;

		Assert.Equal(Colors.Black, firstEntry.TextColor);
		Assert.Equal(Colors.Black, secondEntry.TextColor);
	}
}