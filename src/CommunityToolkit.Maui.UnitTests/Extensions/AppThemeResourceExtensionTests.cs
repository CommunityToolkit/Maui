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
}