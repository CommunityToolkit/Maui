namespace CommunityToolkit.Maui.DeviceTests;

/// <summary>
/// Configuration options for the headless (CI) test runner.
/// Mirrors the pattern used by the .NET MAUI team's TestUtils.DeviceTests.Runners.
/// </summary>
public sealed class HeadlessRunnerOptions
{
	/// <summary>
	/// Whether the tests require a UI context to run.
	/// </summary>
	public bool RequiresUIContext { get; set; } = true;
}
