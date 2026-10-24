namespace CommunityToolkit.Maui.DeviceTests;

// UseMauiCommunityToolkit only touches the page handler on Android (NavigationBar.RemapForControls), so each platform partial asserts its
// own arm. Its iOS .NET 11 block, which connects StatusBar.SetStyle to the windows of the app, is asserted by StatusBarTests.
[Category(TestCategory.AppBuilderNavigationBarRemap)]
[Collection(TestCollections.DisableParallelization)]
public partial class UseMauiCommunityToolkitTests : CommunityToolkitHandlerTestBase
{
}