namespace CommunityToolkit.Maui.DeviceTests;

// Options.SetShouldEnableSnackbarOnWindows has a Windows-only block (#if WINDOWS): every other platform only stores the value. Each
// platform partial tests its own arm.
[Category(TestCategory.SnackbarWindowsSetup)]
[Collection(TestCollections.DisableParallelization)]
public partial class SnackbarWindowsSetupTests : CommunityToolkitHandlerTestBase
{
	[Fact]
	public void SettingTheOptionStoresTheValueOnEveryPlatform()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		try
		{
			var builder = MauiApp.CreateBuilder();

			new Options(builder).SetShouldEnableSnackbarOnWindows(true);
			Assert.True(Options.ShouldEnableSnackbarOnWindows);

			new Options(builder).SetShouldEnableSnackbarOnWindows(false);
			Assert.False(Options.ShouldEnableSnackbarOnWindows);
		}
		finally
		{
			snapshot.Restore();
		}
	}
}