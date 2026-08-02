using System.Diagnostics;

namespace CommunityToolkit.Maui.DeviceTests.Runners;

/// <summary>
/// Service that runs tests headlessly via <see cref="DeviceRunner"/> and reports results to the trace log.
/// Mirrors the pattern used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// </summary>
public sealed class HeadlessRunnerService
{
	readonly HeadlessRunnerOptions options;
	readonly TestOptions testOptions;

	public HeadlessRunnerService(TestOptions testOptions, HeadlessRunnerOptions options)
	{
		this.testOptions = testOptions;
		this.options = options;
	}

	public async Task<int> RunTestsAsync(CancellationToken token = default)
	{
		var runner = new DeviceRunner(testOptions.Assemblies, testOptions.SkipCategories);

		Trace.WriteLine($"Running {testOptions.Assemblies.Count} test assembly(ies) headlessly...");

		await Task.Run(runner.RunAll, token);

		Trace.WriteLine($"Total: {runner.TotalTests}, Passed: {runner.PassedTests}, Failed: {runner.FailedTests}, Skipped: {runner.SkippedTests}");

		foreach (var failure in runner.FailureMessages)
		{
			Trace.WriteLine(failure);
		}

		return runner.FailedTests > 0 ? 1 : 0;
	}
}
