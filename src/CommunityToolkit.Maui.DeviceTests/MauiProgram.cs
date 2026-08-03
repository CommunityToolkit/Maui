using DeviceRunners.VisualRunners;

namespace CommunityToolkit.Maui.DeviceTests;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseVisualTestRunner(conf => conf
				.AddCliConfiguration()
				.AddConsoleResultChannel()
				.AddTestAssembly(typeof(MauiProgram).Assembly)
				.AddXunit())
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		return builder.Build();
	}
}
