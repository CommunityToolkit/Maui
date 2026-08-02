using CommunityToolkit.Maui.DeviceTests.Runners;

namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Extension methods for <see cref="MauiAppBuilder"/> to configure the device test runner.
/// Mirrors the pattern used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// </summary>
public static class AppBuilderExtensions
{
	/// <summary>
	/// Configures the test options (assemblies, skip categories) for the device test runner.
	/// </summary>
	public static MauiAppBuilder ConfigureTests(this MauiAppBuilder builder, Action<TestOptions> configure)
	{
		var options = new TestOptions();
		configure(options);
		builder.Services.AddSingleton(options);
		return builder;
	}

	/// <summary>
	/// Registers the visual in-app test runner as the main page.
	/// </summary>
	public static MauiAppBuilder UseVisualRunner(this MauiAppBuilder builder)
	{
		builder.Services.AddSingleton<VisualRunnerPage>();
		return builder;
	}

	/// <summary>
	/// Registers the headless test runner service for CI/automated test execution.
	/// </summary>
	public static MauiAppBuilder UseHeadlessRunner(this MauiAppBuilder builder, HeadlessRunnerOptions options)
	{
		builder.Services.AddSingleton(options);
		builder.Services.AddSingleton<HeadlessRunnerService>();
		return builder;
	}
}
