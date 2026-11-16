using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

[Category(TestCategory.StatusBar)]
[Collection(TestCollections.DisableParallelization)]
public partial class StatusBarTests : StatusBarTestBase
{
	// Every platform rejects it: Android and iOS as an unknown style, Mac Catalyst and Windows because they have no status bar.
	const StatusBarStyle undefinedStyle = (StatusBarStyle)42;

	[Fact]
	public async Task SetStyleWithUndefinedStyleThrowsNotSupportedException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => Assert.Throws<NotSupportedException>(static () => StatusBar.SetStyle(undefinedStyle)));

		Assert.Equal(UnsupportedStyleMessage, exception.Message);
	}
}