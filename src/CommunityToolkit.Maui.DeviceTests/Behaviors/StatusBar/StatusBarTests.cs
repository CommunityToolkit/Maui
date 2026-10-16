using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Core.Platform;

namespace CommunityToolkit.Maui.DeviceTests;

// Device tests of CommunityToolkit.Maui.Core.Platform.StatusBar: the Android and iOS implementations (the native window state after
// SetColor, SetStyle and the bar size update) and the NotSupportedException of Mac Catalyst and Windows. The shared file holds
// what every platform does the same way; the platform partials read the native state.
[Category(TestCategory.StatusBar)]
[Collection(TestCollections.DisableParallelization)]
public partial class StatusBarTests : StatusBarTestBase
{
	// Not a member of the enum: every platform rejects it (Android and iOS: unknown style, Mac Catalyst and Windows: no status bar).
	const StatusBarStyle undefinedStyle = (StatusBarStyle)42;

	[Fact]
	public async Task SetStyleWithUndefinedStyleThrowsNotSupportedException()
	{
		SetupBuilder();

		var exception = await InvokeOnMainThreadAsync(static () => Assert.Throws<NotSupportedException>(static () => StatusBar.SetStyle(undefinedStyle)));

		Assert.Equal(UnsupportedStyleMessage, exception.Message);
	}
}