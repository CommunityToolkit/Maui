using CommunityToolkit.Maui.Alerts;

namespace CommunityToolkit.Maui.DeviceTests;

public partial class SnackbarWindowsSetupTests
{
	[Fact]
	public void EnablingTheOptionWithoutABuilderIsAcceptedOnThisPlatform()
	{
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		try
		{
			var exception = Record.Exception(() => new Options().SetShouldEnableSnackbarOnWindows(true));

			Assert.Null(exception);
			Assert.True(Options.ShouldEnableSnackbarOnWindows);
		}
		finally
		{
			snapshot.Restore();
		}
	}

	[Fact]
	public void EnablingTheOptionRegistersNothingOnTheBuilderOnThisPlatform()
	{
		var builder = MauiApp.CreateBuilder();
		var serviceCount = builder.Services.Count;
		var snapshot = new StaticPropertySnapshot().Capture(typeof(Options)).Capture(typeof(Core.Options));
		try
		{
			new Options(builder).SetShouldEnableSnackbarOnWindows(true);

			Assert.Equal(serviceCount, builder.Services.Count);
		}
		finally
		{
			snapshot.Restore();
		}
	}

	[Fact]
	public async Task SnackbarConstructorDoesNotNeedTheWindowsOptionOnThisPlatform()
	{
		SetupBuilder(configureToolkitOptions: static options => options.SetShouldEnableSnackbarOnWindows(false));

		await InvokeOnMainThreadAsync(() =>
		{
			Assert.False(Options.ShouldEnableSnackbarOnWindows);

			var exception = Record.Exception(() => new Snackbar());

			Assert.Null(exception);
		});
	}
}