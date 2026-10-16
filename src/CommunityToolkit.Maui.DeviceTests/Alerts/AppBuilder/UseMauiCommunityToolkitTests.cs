namespace CommunityToolkit.Maui.DeviceTests;

// AppBuilderExtensions.UseMauiCommunityToolkit has one platform block for the page handler: on Android (#if ANDROID) it calls
// NavigationBar.RemapForControls, which maps the toolkit's NavigationBar.Color and NavigationBar.Style attached properties onto the Controls
// page handler. Every other platform leaves the page handler alone. The shared part only holds the class; each platform partial asserts its
// own arm. (Its iOS .NET 11 block, which connects StatusBar.SetStyle to the windows of the app, is asserted by StatusBarTests.)
[Category(TestCategory.AppBuilderNavigationBarRemap)]
[Collection(TestCollections.DisableParallelization)]
public partial class UseMauiCommunityToolkitTests : CommunityToolkitHandlerTestBase
{
}