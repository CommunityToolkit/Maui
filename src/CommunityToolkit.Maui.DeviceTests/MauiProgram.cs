using System.Reflection;
using CommunityToolkit.Maui.DeviceTests;

namespace CommunityToolkit.Maui.DeviceTests;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();

		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.ConfigureTests(options =>
		{
			options.Assemblies =
			[
				Assembly.GetExecutingAssembly(),
			];
		});

		builder.UseVisualRunner();

		builder.UseHeadlessRunner(new HeadlessRunnerOptions
		{
			RequiresUIContext = true,
		});

		return builder.Build();
	}
}
